shader_type canvas_item;

// 保留一代原图像素与配色；用二代原生轮廓露出插画，适配其卡面分层。
uniform sampler2D frame_atlas : source_color, filter_linear, repeat_disable;
uniform vec4 source_region;
uniform vec4 frame_region;
uniform bool slot_background = false;
varying vec4 item_color;

void vertex() {
    item_color = COLOR;
}

void fragment() {
    vec2 local_uv = (UV - source_region.xy) / source_region.zw;
    vec4 original = texture(TEXTURE, UV);
    float frame_alpha = texture(frame_atlas, frame_region.xy + local_uv * frame_region.zw).a;
    // 收藏卡背保留原图；仅为卡牌下半部正文增加对比，不改动插画与原生文字层。
    float readability = slot_background ? mix(1.0, 0.28, smoothstep(0.47, 0.57, local_uv.y)) : 1.0;
    COLOR = vec4(original.rgb * readability, original.a * frame_alpha) * item_color;
}
