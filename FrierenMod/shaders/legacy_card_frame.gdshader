shader_type canvas_item;

// 保留一代原图像素与配色；用二代原生轮廓露出插画，适配其卡面分层。
uniform sampler2D frame_atlas : source_color, filter_linear, repeat_disable;
uniform vec4 source_region;
uniform vec4 frame_region;
varying vec4 item_color;

void vertex() {
    item_color = COLOR;
}

void fragment() {
    vec2 local_uv = (UV - source_region.xy) / source_region.zw;
    vec4 original = texture(TEXTURE, UV);
    float frame_alpha = texture(frame_atlas, frame_region.xy + local_uv * frame_region.zw).a;
    COLOR = vec4(original.rgb, original.a * frame_alpha) * item_color;
}
