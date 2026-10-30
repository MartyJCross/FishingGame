// Things below the surface (bank, lake bed, plants, cover; docs/07 §6.4-6.5): fogged toward the water's colour with
// depth (clear springs show far, the pond fogs fast), and the lake bed lit by moving caustics in sunlit shallows.
// Anything above the water is untouched. Local pixels are world pixels.
shader_type canvas_item;

uniform vec4 water_col : source_color;
uniform float visibility = 1.8;
uniform float direct = 1.0;
uniform vec4 sun_tint : source_color = vec4(1.0, 0.97, 0.88, 1.0);
uniform float caustics = 1.0;
uniform vec2 profile[16];
uniform int profile_count = 0;

varying vec2 wp;

void vertex() {
	wp = VERTEX / 120.0 * vec2(1.0, -1.0);
}

float ground(float x) {
	if (profile_count < 2) return -100.0;
	if (x <= profile[0].x) return profile[0].y;
	for (int i = 1; i < 16; i++) {
		if (i >= profile_count) break;
		vec2 a = profile[i - 1];
		vec2 b = profile[i];
		if (x <= b.x) return mix(a.y, b.y, (x - a.x) / max(b.x - a.x, 1e-4));
	}
	return profile[profile_count - 1].y;
}

// A tileable caustic network (a classic iterated-warp pattern): bright thin lines that wander.
float caustic(vec2 p) {
	vec2 q = mod(p * 6.2831, 6.2831) - 250.0;
	vec2 i = q;
	float c = 1.0;
	float inten = 0.005;
	for (int n = 0; n < 4; n++) {
		float t = TIME * 0.45 * (1.0 - (3.5 / float(n + 1)));
		i = q + vec2(cos(t - i.x) + sin(t + i.y), sin(t - i.y) + cos(t + i.x));
		c += 1.0 / length(vec2(q.x / (sin(i.x + t) / inten), q.y / (cos(i.y + t) / inten)));
	}
	c /= 4.0;
	c = 1.17 - pow(c, 1.4);
	return clamp(pow(abs(c), 8.0), 0.0, 1.0);
}

void fragment() {
	vec4 col = COLOR; // already vertex colour x texture
	if (wp.y < -0.03) {
		float depth = -wp.y;
		// Distance below the ground surface: the cut-away earth deep down stays earth-coloured.
		float below = ground(wp.x) - wp.y;
		float earth = smoothstep(0.35, 1.4, below);
		float fog = (1.0 - exp(-depth / (visibility * 2.4 + 0.3))) * 0.88;
		fog = max(fog, 0.18) * (1.0 - 0.75 * earth);
		col.rgb = mix(col.rgb, water_col.rgb, fog);
		float bed = 1.0 - smoothstep(0.04, 0.4, below);
		float lit = caustic(wp * vec2(0.33, 0.5)) * direct * caustics * exp(-depth / (visibility * 1.4 + 0.4)) * bed;
		col.rgb += sun_tint.rgb * lit * 0.45;
	}
	COLOR = col;
}
