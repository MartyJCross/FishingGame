// The sky (docs/07_ART_DIRECTION.md §7): a full-screen gradient driven by the time-of-day palette, the sun and its
// glow, the moon and stars at night, and soft banded clouds whose coverage follows the weather. Screen space; the
// horizon and the cloud field are placed in world metres so they sit right at any camera zoom.
shader_type canvas_item;
render_mode unshaded;

uniform vec4 sky_top : source_color;
uniform vec4 sky_horizon : source_color;
uniform vec4 sun_color : source_color;
uniform vec4 cloud_lit : source_color;
uniform vec4 cloud_shade : source_color;
uniform vec2 sun_uv;
uniform float sun_visible;
uniform vec2 moon_uv;
uniform float moon_visible;
uniform float stars;
uniform float horizon_uv;
uniform float aspect = 1.777;
uniform float metres_per_uv = 9.0;
uniform float cam_x;
uniform float cloud_cover = 0.2;
uniform float cloud_drift;
uniform int octaves = 5;

float hash(vec2 p) {
	p = fract(p * vec2(123.34, 456.21));
	p += dot(p, p + 45.32);
	return fract(p.x * p.y);
}

float noise(vec2 p) {
	vec2 i = floor(p);
	vec2 f = fract(p);
	vec2 u = f * f * (3.0 - 2.0 * f);
	return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x), mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x), u.y);
}

float fbm(vec2 p) {
	float v = 0.0;
	float a = 0.5;
	for (int i = 0; i < octaves; i++) {
		v += a * noise(p);
		p = p * 2.03 + vec2(17.1, 9.2);
		a *= 0.5;
	}
	return v;
}

void fragment() {
	vec2 uv = UV;
	float above = clamp((horizon_uv - uv.y) / max(horizon_uv, 0.05), 0.0, 1.0);
	vec3 col = mix(sky_horizon.rgb, sky_top.rgb, pow(above, 0.55));

	// Stars, fading toward the horizon haze.
	vec2 sp = uv * vec2(aspect, 1.0) * 150.0;
	vec2 cell = floor(sp);
	float h = hash(cell);
	if (h > 0.982 && stars > 0.0) {
		vec2 c = fract(sp) - 0.5 - (vec2(hash(cell + 3.1), hash(cell + 7.7)) - 0.5) * 0.6;
		float tw = 0.55 + 0.45 * sin(TIME * (0.8 + h * 3.0) + h * 40.0);
		float s = smoothstep(0.11 + 0.06 * fract(h * 91.0), 0.0, length(c)) * tw;
		col += vec3(0.9, 0.93, 1.0) * s * stars * smoothstep(0.05, 0.4, above);
	}

	// Sun: wide glow, a warm band along the horizon on its side of the sky, and the disc.
	vec2 ds = (uv - sun_uv) * vec2(aspect, 1.0);
	float d = length(ds);
	col += sun_color.rgb * (exp(-d * 2.6) * 0.3 + exp(-d * 13.0) * 0.3) * sun_visible;
	col += sun_color.rgb * 0.3 * sun_visible * exp(-max(uv.y - horizon_uv, horizon_uv - uv.y) * 7.0) * exp(-abs(ds.x) * 1.1);
	col = mix(col, vec3(1.0, 0.98, 0.92) * 0.5 + sun_color.rgb * 0.6, smoothstep(0.036, 0.031, d) * sun_visible);

	// Moon: a soft halo and a pale disc with a darker mare or two.
	vec2 dm = (uv - moon_uv) * vec2(aspect, 1.0);
	float md = length(dm);
	col += vec3(0.62, 0.7, 0.95) * exp(-md * 9.0) * 0.22 * moon_visible;
	float disc = smoothstep(0.022, 0.019, md);
	float mare = noise(dm * 260.0 + 3.0) * 0.6 + noise(dm * 520.0) * 0.4;
	col = mix(col, vec3(0.92, 0.93, 0.88) - smoothstep(0.5, 0.75, mare) * 0.1, disc * moon_visible);

	// Clouds in world metres above the horizon, drifting with the wind; tops lit, bellies shaded, in flat bands.
	vec2 wp = vec2((uv.x - 0.5) * aspect * metres_per_uv + cam_x * 0.25 + cloud_drift, (horizon_uv - uv.y) * metres_per_uv);
	vec2 q = vec2(wp.x * 0.11, wp.y * 0.34);
	float dens = fbm(q);
	float thr = mix(0.6, 0.3, cloud_cover);
	float band = smoothstep(0.8, 2.2, wp.y);
	float cover = smoothstep(thr, thr + 0.1, dens) * band;
	if (cover > 0.001) {
		float up = fbm(q + vec2(0.04, 0.16));
		float lit = clamp((dens - up) * 5.0 + 0.55, 0.0, 1.0);
		lit = floor(lit * 3.0 + 0.5) / 3.0;
		vec3 cc = mix(cloud_shade.rgb, cloud_lit.rgb, lit);
		// Silver lining near the sun.
		cc += sun_color.rgb * exp(-d * 5.0) * 0.35 * sun_visible * (1.0 - cover * 0.5);
		col = mix(col, cc, cover * mix(0.9, 1.0, cloud_cover));
	}
	COLOR = vec4(col, 1.0);
}
