// The near surface band (docs/07 §6.2-6.3): the top of the water seen at a low angle. Sky reflection from near to
// far, the far shore's dark reflection along the back edge, and a glitter path of sparkles under the sun or moon
// that widens as the wind roughens the water. UV.y runs 0 at the waterline to 1 at the band's back edge.
shader_type canvas_item;

uniform vec4 near_col : source_color;
uniform vec4 far_col : source_color;
uniform vec4 shore_col : source_color;
uniform vec4 glint_col : source_color = vec4(1.0, 0.95, 0.8, 1.0);
uniform float sun_x_m = 0.0;
uniform float glint = 1.0;
uniform float rough = 0.2;

varying float wx;

void vertex() {
	wx = VERTEX.x / 120.0;
}

float hash(vec2 p) {
	p = fract(p * vec2(123.34, 456.21));
	p += dot(p, p + 45.32);
	return fract(p.x * p.y);
}

void fragment() {
	float v = UV.y;
	vec3 c = mix(near_col.rgb, far_col.rgb, smoothstep(0.0, 1.0, v));
	// The far shore's reflection: a darker strip at the back, broken up by the wind.
	float shore = smoothstep(0.62, 0.95, v) * (0.7 - 0.4 * rough);
	shore *= 0.85 + 0.15 * sin(wx * 9.0 + TIME * 1.3 + v * 30.0);
	c = mix(c, shore_col.rgb, shore);
	// Wind streaks (cat's-paws): faint lighter lines across the band.
	c += vec3(0.06) * rough * smoothstep(0.85, 1.0, sin(v * 70.0 + sin(wx * 0.7 + TIME * 0.4) * 3.0));
	// Glitter path: a soft glow and short twinkling dashes toward the light's side.
	float width = 1.0 + 6.0 * rough + 3.0 * v;
	float path = exp(-abs(wx - sun_x_m) / width);
	vec2 g = vec2(wx * 16.0, v * 13.0);
	vec2 cell = floor(g);
	vec2 f = fract(g) - 0.5;
	float h = hash(cell);
	float tw = smoothstep(0.75, 1.0, sin(TIME * (2.0 + 4.0 * h + 3.0 * rough) + h * 60.0));
	float dash = smoothstep(0.42, 0.15, abs(f.x)) * smoothstep(0.28, 0.08, abs(f.y));
	float sparkle = step(0.82, h) * tw * dash * path;
	c += glint_col.rgb * glint * (path * 0.22 * (1.0 - v * 0.5) + sparkle * 0.9);
	COLOR = vec4(c, COLOR.a);
}
