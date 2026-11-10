// 读取项目使用的8位RGB PSD（RAW/RLE），保留图层参数及合成预览。
export function psdReader(buffer) {
  let position = 0;
  const bytes = n => { const b = buffer.subarray(position, position + n); position += n; return b; };
  const u16 = () => bytes(2).readUInt16BE();
  const i16 = () => bytes(2).readInt16BE();
  const u32 = () => bytes(4).readUInt32BE();
  const i32 = () => bytes(4).readInt32BE();
  if (bytes(4).toString() !== '8BPS' || u16() !== 1) throw Error('只支持 PSD v1');
  bytes(6);
  const channelCount = u16(), height = u32(), width = u32(), depth = u16(), mode = u16();
  if (depth !== 8 || mode !== 3) throw Error('只支持 8 位 RGB PSD');
  bytes(u32()); bytes(u32());
  const sectionLength = u32(), sectionStart = position;
  const infoLength = u32(), infoStart = position;
  const count = Math.abs(i16());
  const layers = [];
  for (let k = 0; k < count; k++) {
    const top = i32(), left = i32(), bottom = i32(), right = i32(), channels = [];
    for (let n = u16(); n > 0; n--) channels.push({ id: i16(), length: u32() });
    if (bytes(4).toString() !== '8BIM') throw Error('图层签名无效');
    const blend = bytes(4).toString(), opacity = bytes(1)[0], clipping = bytes(1)[0], flags = bytes(1)[0];
    bytes(1);
    const extraLength = u32(), end = position + extraLength;
    bytes(u32()); bytes(u32());
    const nameLength = bytes(1)[0];
    let name = bytes(nameLength).toString('latin1');
    bytes((4 - (nameLength + 1) % 4) % 4);
    while (position + 12 <= end) {
      bytes(4); const key = bytes(4).toString(), len = u32(), data = bytes(len); bytes(len % 2);
      if (key === 'luni') {
        name = '';
        for (let c = 0; c < data.readUInt32BE(); c++) name += String.fromCharCode(data.readUInt16BE(4 + c * 2));
      }
    }
    position = end;
    layers.push({ name, top, left, width: right - left, height: bottom - top, channels, blend, opacity, clipping, visible: !(flags & 2) });
  }
  function decode(data, w, h, planes = 1) {
    const compression = data.readUInt16BE();
    if (compression === 0) return data.subarray(2, 2 + w * h * planes);
    if (compression !== 1) throw Error(`PSD 压缩方式 ${compression} 暂不支持`);
    const rows = h * planes, output = Buffer.alloc(w * rows);
    let cursor = 2 + rows * 2;
    for (let y = 0; y < rows; y++) {
      const end = cursor + data.readUInt16BE(2 + y * 2);
      let target = y * w;
      while (cursor < end) {
        const n = data.readInt8(cursor++);
        if (n >= 0) { data.copy(output, target, cursor, cursor + n + 1); target += n + 1; cursor += n + 1; }
        else if (n !== -128) { const value = data[cursor++]; output.fill(value, target, target + 1 - n); target += 1 - n; }
      }
      if (target !== (y + 1) * w) throw Error('PSD RLE 行长度错误');
    }
    return output;
  }
  function rgba(planes, w, h) {
    const size = w * h, out = Buffer.alloc(size * 4);
    for (let p = 0; p < size; p++) {
      for (let c = 0; c < 3; c++) out[p * 4 + c] = planes[c]?.[p] ?? 0;
      out[p * 4 + 3] = planes[-1]?.[p] ?? 255;
    }
    return out;
  }
  for (const layer of layers) {
    const planes = {};
    for (const channel of layer.channels) planes[channel.id] = decode(bytes(channel.length), layer.width, layer.height);
    layer.rgba = rgba(planes, layer.width, layer.height);
  }
  position = sectionStart + sectionLength;
  const mergedData = decode(bytes(buffer.length - position), width, height, channelCount), planes = {}, size = width * height;
  for (let c = 0; c < Math.min(channelCount, 4); c++) planes[c === 3 ? -1 : c] = mergedData.subarray(c * size, (c + 1) * size);
  return { width, height, layers, merged: rgba(planes, width, height) };
}
