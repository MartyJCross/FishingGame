// A soft frame (docs/07 §10): darkens the corners a little so the eye sits on the water.
shader_type canvas_item;

uniform float strength = 0.32;
uniform vec4 tint : source_color = vec4(0.0, 0.02, 0.05, 1.0);

void fragment() {
	vec2 d = (UV - 0.5) * vec2(1.0, 0.82);
	float v = smoothstep(0.32, 0.78, length(d) * 1.2);
	COLOR = vec4(tint.rgb, v * strength);
}
