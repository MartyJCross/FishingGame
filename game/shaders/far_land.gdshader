// Far land (docs/07 §3 layers 3-4): flat silhouettes lit by the time-of-day light and blended toward the horizon
// haze (atmospheric perspective). Low-lying mist gathers along the water at dawn and in damp weather.
shader_type canvas_item;

uniform vec4 light_col : source_color = vec4(1.0);
uniform vec4 haze_col : source_color = vec4(0.8, 0.86, 0.9, 1.0);
uniform float haze = 0.3;
uniform float mist = 0.0;
/// World y of the water line in local pixels (y down): mist is thickest here.
uniform float mist_base_px = -36.0;

varying float local_y;

void vertex() {
	local_y = VERTEX.y;
}

void fragment() {
	vec4 c = COLOR;
	float m = mist * clamp(1.0 - (mist_base_px - local_y) / 140.0, 0.0, 1.0);
	c.rgb = mix(c.rgb * light_col.rgb, haze_col.rgb, clamp(haze + m, 0.0, 1.0));
	COLOR = c;
}
