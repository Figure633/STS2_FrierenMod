"""从指定源提取BGM，只裁首尾静音，并进行双遍响度匹配。原文件始终只读。"""
import argparse, concurrent.futures, hashlib, json, os, re, subprocess, sys
from pathlib import Path

ROOT=Path(__file__).resolve().parent.parent

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--downloads',type=Path,required=True)
    parser.add_argument('--legacy',type=Path,required=True)
    parser.add_argument('--ffmpeg',type=Path)
    parser.add_argument('--jobs',type=int,default=2)
    args=parser.parse_args()
    if args.ffmpeg: ffmpeg=str(args.ffmpeg)
    else:
        sys.path.insert(0,str(ROOT/'artifacts/audio-tools'))
        import imageio_ffmpeg
        ffmpeg=imageio_ffmpeg.get_ffmpeg_exe()
    manifest=json.loads((ROOT/'tools/music_sources.json').read_text(encoding='utf-8'))
    out=ROOT/'FrierenMod/audio/bgm';out.mkdir(parents=True,exist_ok=True)
    report_path=ROOT/'docs/inventory/bgm_audio.json'
    old={t['id']:t for t in json.loads(report_path.read_text(encoding='utf-8'))['tracks']} if report_path.exists() else {}
    def run(params):
        result=subprocess.run([ffmpeg,'-hide_banner','-nostdin','-threads','1',*params],capture_output=True,text=True,encoding='utf-8',errors='replace')
        if result.returncode: raise RuntimeError(result.stderr[-3000:])
        return result.stderr
    def loudness(stderr):
        hits=re.findall(r'\{\s*"input_i".*?\}',stderr,re.S)
        if not hits: raise RuntimeError('FFmpeg没有返回响度测量')
        return json.loads(hits[-1])
    def process(track):
        kind,name=track['source'].split(':',1)
        source=(args.downloads if kind=='download' else args.legacy)/name
        digest=hashlib.sha256(source.read_bytes()).hexdigest()
        target=out/track['file']
        previous=old.get(track['id'])
        if previous and previous['sourceSha256']==digest and target.exists() and hashlib.sha256(target.read_bytes()).hexdigest()==previous['sha256']:
            print('CACHE',track['id'],flush=True);return previous
        analysis=run(['-i',str(source),'-map','0:a:0','-af','silencedetect=noise=-55dB:d=0.35','-f','null','-'])
        m=re.search(r'Duration: (\d+):(\d+):([\d.]+)',analysis)
        if not m: raise RuntimeError('无法识别时长：'+str(source))
        duration=int(m[1])*3600+int(m[2])*60+float(m[3])
        silence=[(event,float(value)) for event,value in re.findall(r'silence_(start|end):\s*([\d.]+)',analysis)]
        start=0.0;end=duration
        if len(silence)>=2 and silence[0][0]=='start' and silence[0][1]<0.05 and silence[1][0]=='end':
            start=max(0,silence[1][1]-.03)
        if silence:
            if silence[-1][0]=='start': end=min(duration,silence[-1][1]+.03)
            elif len(silence)>=2 and silence[-1][1]>=duration-.1 and silence[-2][0]=='start': end=min(duration,silence[-2][1]+.03)
        if end-start<10: raise RuntimeError('有效音频过短，停止处理：'+name)
        trim=f'atrim=start={start:.6f}:end={end:.6f},asetpts=PTS-STARTPTS'
        norm=f'loudnorm=I={manifest["targetLufs"]}:TP={manifest["truePeakDb"]}:LRA=50'
        measured=loudness(run(['-i',str(source),'-map','0:a:0','-af',trim+','+norm+':print_format=json','-f','null','-']))
        linear=norm+':measured_I='+measured['input_i']+':measured_TP='+measured['input_tp']+':measured_LRA='+measured['input_lra']+':measured_thresh='+measured['input_thresh']+':offset='+measured['target_offset']+':linear=true'
        temp=target.with_suffix('.tmp.ogg')
        run(['-y','-i',str(source),'-map','0:a:0','-vn','-af',trim+','+linear,'-c:a','libvorbis','-q:a','5','-ar','48000','-ac','2','-map_metadata','-1','-metadata','title='+track['title'],str(temp)])
        temp.replace(target)
        verified=loudness(run(['-i',str(target),'-af',norm+':print_format=json','-f','null','-']))
        result={**track,'sourceSha256':digest,'sha256':hashlib.sha256(target.read_bytes()).hexdigest(),'durationSeconds':round(end-start,3),'trimStartSeconds':round(start,3),'trimEndSeconds':round(duration-end,3),'integratedLufs':float(verified['input_i']),'truePeakDb':float(verified['input_tp'])}
        if abs(result['integratedLufs']-manifest['targetLufs'])>1 or result['truePeakDb']>-.3:
            raise RuntimeError('响度检查未通过：'+json.dumps(result,ensure_ascii=False))
        print('READY',track['id'],result['durationSeconds'],'seconds',result['integratedLufs'],'LUFS',flush=True)
        return result
    with concurrent.futures.ThreadPoolExecutor(max_workers=args.jobs) as pool:
        results=list(pool.map(process,manifest['tracks']))
    report_path.write_text(json.dumps({'targetLufs':manifest['targetLufs'],'tracks':results},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    print('MUSIC_READY',len(results),flush=True)

if __name__=='__main__':main()
