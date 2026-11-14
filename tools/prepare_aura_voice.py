"""只读作者原音频，裁去首尾静音并双遍匹配阿乌拉战语音响度。"""
import argparse
import hashlib
import json
import math
from pathlib import Path
import re
import subprocess

ROOT = Path(__file__).resolve().parent.parent
SOURCES = {
    'aura_opening': '阿乌拉战开场语音：我会亲自砍下你的头.MP3',
    'frieren_opening': '芙莉莲在阿乌拉开场白说完后：你把我的灵魂放上天平了啊，阿乌拉.MP3',
    'aura_success': '阿乌拉服从魔法成功时：是我赢了.MP3',
    'aura_failure': '阿乌拉服从魔法失败时：你是笨蛋吗？.MP3',
    'aura_transition': '阿乌拉转阶段语音：别开玩笑了，我可是活了超越五百年以上的大魔族.MP3',
    'aura_taunt': '嘲讽回合：看来我根本没必要消耗你的魔力....MP3',
    'aura_death': '阿乌拉死亡音效：...我竟然....MP3',
    'frieren_victory': '芙莉莲击败阿乌拉语音：阿乌拉，在你面前的是，活了超越千年的魔法使.MP3',
}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source-dir', type=Path, required=True)
    parser.add_argument('--ffmpeg', type=Path, required=True)
    args = parser.parse_args()
    output = ROOT / 'FrierenMod/audio/aura'
    output.mkdir(parents=True, exist_ok=True)
    target_lufs, peak = -16, -2
    tracks = []

    def run(args_):
        result = subprocess.run([str(args.ffmpeg), '-hide_banner', '-nostdin', '-threads', '1', *args_],
                                capture_output=True, text=True, encoding='utf-8', errors='replace')
        if result.returncode:
            raise RuntimeError(result.stderr[-3000:])
        return result.stderr

    def measure(text):
        return json.loads(re.findall(r'\{\s*"input_i".*?\}', text, re.S)[-1])

    for voice, filename in SOURCES.items():
        source = args.source_dir / filename
        analysis = run(['-i', str(source), '-af', 'silencedetect=noise=-50dB:d=0.15', '-f', 'null', '-'])
        duration_match = re.search(r'Duration: (\d+):(\d+):([\d.]+)', analysis)
        if not duration_match:
            raise RuntimeError('无法读取音频时长：' + filename)
        h, m, s = duration_match.groups()
        duration = int(h) * 3600 + int(m) * 60 + float(s)
        silence = [(event, float(value)) for event, value in re.findall(r'silence_(start|end):\s*([\d.]+)', analysis)]
        start, end = 0.0, duration
        if len(silence) >= 2 and silence[0][0] == 'start' and silence[0][1] < .05 and silence[1][0] == 'end':
            start = max(0, silence[1][1] - .05)
        if silence:
            if silence[-1][0] == 'start':
                end = min(duration, silence[-1][1] + .05)
            elif len(silence) >= 2 and silence[-1][1] >= duration - .1 and silence[-2][0] == 'start':
                end = min(duration, silence[-2][1] + .05)
        if end - start < .4:
            raise RuntimeError('有效语音过短，停止：' + filename)
        trim = f'atrim=start={start:.6f}:end={end:.6f},asetpts=PTS-STARTPTS,aformat=channel_layouts=mono'
        norm = f'loudnorm=I={target_lufs}:TP={peak}:LRA=11'
        measured = measure(run(['-i', str(source), '-af', trim + ',' + norm + ':print_format=json', '-f', 'null', '-']))
        if not math.isfinite(float(measured['input_i'])):
            raise RuntimeError('语音响度测量失败：' + filename)
        linear = norm + ''.join(':' + key + '=' + measured[value] for key, value in [
            ('measured_I', 'input_i'), ('measured_TP', 'input_tp'), ('measured_LRA', 'input_lra'),
            ('measured_thresh', 'input_thresh'), ('offset', 'target_offset')]) + ':linear=true'
        target = output / (voice + '.ogg')
        correction = 0.0
        for attempt in range(4):
            # 短句的 EBU 门限与高峰值会使首轮偏低；从原文件重做校正，以前瞻限幅留足编码峰值余量。
            filters = trim + ',' + linear + f',volume={correction:.4f}dB,alimiter=limit=0.749894:level=false:latency=true'
            run(['-y', '-i', str(source), '-vn', '-af', filters,
                 '-c:a', 'libvorbis', '-q:a', '5', '-ar', '48000', '-ac', '1', '-map_metadata', '-1', str(target)])
            verified = measure(run(['-i', str(target), '-af', norm + ':print_format=json', '-f', 'null', '-']))
            integrated, true_peak = float(verified['input_i']), float(verified['input_tp'])
            if abs(integrated-target_lufs) < .3 and true_peak <= -1.5:
                break
            correction += target_lufs-integrated
        if abs(integrated - target_lufs) > .6 or true_peak > -1.5:
            raise RuntimeError('响度或峰值未达标：' + voice + ' ' + str(verified))
        tracks.append(dict(id=voice, file=target.relative_to(ROOT).as_posix(), sourceFile=filename,
                           sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),
                           sha256=hashlib.sha256(target.read_bytes()).hexdigest(),
                           durationSeconds=round(end-start, 4), trimStartSeconds=round(start, 4),
                           trimEndSeconds=round(duration-end, 4), integratedLufs=integrated, truePeakDb=true_peak))
        print(f'{voice}: {end-start:.3f}s / {integrated:.2f} LUFS / {true_peak:.2f} dBTP', flush=True)

    report = dict(targetLufs=target_lufs, truePeakTargetDb=peak, encodedTruePeakLimitDb=-1.5, channels=1, sampleRate=48000, tracks=tracks)
    (ROOT / 'tools/data/aura_voice.json').write_text(json.dumps(report, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')
    asset_path = ROOT / 'tools/data/assets.json'
    assets = json.loads(asset_path.read_text(encoding='utf-8-sig'))
    assets = [a for a in assets if a['file'] not in {t['file'] for t in tracks}]
    assets.extend(dict(file=t['file'], source=t['sourceFile'], source_kind='author_provided', modified=True,
                       processing='tools/prepare_aura_voice.py; trim only leading/trailing silence, mono, two-pass loudness normalization and peak-limited short-clip correction; measurements in tools/data/aura_voice.json',
                       sha256=t['sha256']) for t in tracks)
    asset_path.write_text(json.dumps(assets, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')


if __name__ == '__main__':
    main()
