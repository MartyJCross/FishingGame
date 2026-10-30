// Rain streaks over the scene (docs/07 §3 layer 9): two depths of fine slanted streaks, denser with the rain rate,
// slanting with the wind. Screen space.
shader_type canvas_item;

uniform float amount = 0.0;
uniform float slant = 0.15;
uniform float aspect = 1.777;
uniform vec4 tint : source_color = vec4(0.82, 0.86, 0.92, 1.0);

float hash(vec2 p) {
	p = fract(p * vec2(123.34, 456.21));
	p += dot(p, p + 45.32);
	return fract(p.x * p.y);
}

void fragment() {
	float a = 0.0;
	for (int layer = 0; layer < 2; layer++) {
		float fl = float(layer);
		vec2 p = vec2(UV.x * aspect, UV.y);
		p.x += p.y * slant;
		p *= vec2(70.0 + fl * 45.0, 3.0 + fl * 1.5);
		p.y -= TIME * (5.5 + fl * 2.5);
		vec2 cell = floor(p);
		vec2 f = fract(p);
		float h = hash(cell + fl * 31.0);
		if (h < amount * (0.45 - fl * 0.12)) {
			float x = f.x - 0.5 - (hash(cell + 7.0) - 0.5) * 0.7;
			float len = 0.35 + 0.3 * hash(cell + 3.0);
			float start = hash(cell + 11.0) * (1.0 - len);
			float streak = smoothstep(0.08, 0.0, abs(x)) * step(start, f.y) * step(f.y, start + len);
			a += streak * (0.22 - fl * 0.07);
		}
	}
	COLOR = vec4(tint.rgb, clamp(a, 0.0, 0.5));
}
