// The water volume (docs/07 §6.4-6.5): colour by depth, god rays slanting from the sun's side that shimmer and fade
// with depth and murk, the bright underside of the surface, and drifting specks. Local pixels are world pixels.
shader_type canvas_item;

uniform vec4 shallow : source_color;
uniform vec4 deep : source_color;
uniform float grad_depth = 5.0;
uniform float visibility = 1.8;
uniform float direct = 1.0;
uniform vec4 sun_tint : source_color = vec4(1.0, 0.97, 0.88, 1.0);
/// Horizontal metres per metre of depth the rays travel (+ = sun on the left).
uniform float sun_slope = 0.4;
uniform float rays = 1.0;
uniform float specks = 1.0;

varying vec2 wp;

void vertex() {
	wp = VERTEX / 120.0 * vec2(1.0, -1.0);
}

float hash(vec2 p) {
	p = fract(p * vec2(123.34, 456.21));
	p += dot(p, p + 45.32);
	return fract(p.x * p.y);
}

float noise1(float x) {
	float i = floor(x);
	float f = fract(x);
	float u = f * f * (3.0 - 2.0 * f);
	return mix(hash(vec2(i, 1.7)), hash(vec2(i + 1.0, 1.7)), u);
}

void fragment() {
	float depth = max(-wp.y, 0.0);
	vec4 c = mix(shallow, deep, smoothstep(0.0, grad_depth, depth));
	c.a = mix(shallow.a, deep.a, smoothstep(0.0, grad_depth, depth));

	// God rays: bands along the sun's direction, two slow layers multiplied so they come and go.
	float s = wp.x - depth * sun_slope;
	float r = noise1(s * 1.9 + TIME * 0.09) * noise1(s * 0.55 - TIME * 0.05 + 3.0);
	r = smoothstep(0.18, 0.7, r);
	float fade = exp(-depth / (visibility * 1.2 + 0.6));
	c.rgb += sun_tint.rgb * r * fade * direct * 0.2 * rays;

	// The underside of the surface catches the light.
	c.rgb += vec3(0.62, 0.82, 0.86) * exp(-depth * 16.0) * 0.16 * (0.35 + 0.65 * direct);

	// Specks of plankton and silt drifting in the light.
	vec2 g = wp * 8.0 + vec2(TIME * 0.06, sin(TIME * 0.21 + wp.x) * 0.12);
	vec2 cell = floor(g);
	float h = hash(cell);
	if (h > 0.94 && specks > 0.0) {
		vec2 f = fract(g) - 0.5 - (vec2(hash(cell + 1.3), hash(cell + 4.1)) - 0.5) * 0.7;
		c.rgb += vec3(0.8, 0.9, 0.82) * smoothstep(0.08, 0.0, length(f)) * 0.16 * specks * (0.3 + 0.7 * direct) * (0.4 + 0.6 * fade);
	}
	COLOR = c;
}
