function vertexShaderRingTexture() {
    return [
        'uniform float uvScale;', // Uniform to control UV scaling
        'varying vec2 vUv;',
        'void main() {',
        'vUv = uv * uvScale;', // Scale the UV coordinates
        'gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);',
        '}'
    ].join('\n');
}

function fragShaderRingTexture() {
    return [
        'uniform sampler2D dustTexture;',
        'uniform float discardProbability;', // Uniform to control discard probability
        'uniform float noiseIntensity;', // Uniform to control noise intensity
        'varying vec2 vUv;',
        'float random(vec2 co) {',
        '    return fract(sin(dot(co.xy, vec2(12.9898, 78.233))) * 43758.5453);',
        '}',
        'void main() {',
        '    vec4 color = texture2D(dustTexture, vUv);',
        // Calculate the lightness of the pixel
        '    float lightness = 0.2126 * color.r + 0.7152 * color.g + 0.0722 * color.b;',
        // Generate a random value based on the fragment coordinates
        '    float rand = random(gl_FragCoord.xy);',
        // Discard the pixel if its lightness is below the threshold or based on randomness
        '    if (lightness < 0.1 || rand < discardProbability) discard;',
        // Introduce noise to create roughness
        '    float noise = random(vUv * 100.0) * noiseIntensity;',
        '    color.rgb += noise * 0.1;', // Adjust the intensity of the noise
        '    gl_FragColor = color;',
        '}'
    ].join('\n');
}


const textureLoader = new THREE.TextureLoader();
const dustTexture = textureLoader.load('/Textures/ringDust.jpg');

function uniformsRingTexture(colorScheme, discardProb) {
    return {
        dustTexture: { value: dustTexture },
        color: colorScheme,
        discardProbability: { value: discardProb }, //controlls the cahnge or a random pixel in the texture beeing discarded
        noiseIntensity: { value: 0.5 }, //controlls the intensity or random pixels for rougness
        uvScale: { value: 0.5 } // Adjust this value to control the UV scaling (how the texutre is overlayed)
    };
}

function vertexShaderRingPixel() {
    return [
        'varying vec2 vUv;',
        'void main() {',
        'vUv = uv;',
        'gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);',
        '}'
    ].join('\n');
}

function fragShaderRingPixel() {
    return [
        'uniform float discardProbability;', // Uniform to control discard probability
        'uniform float noiseIntensity;', // Uniform to control noise intensity
        'uniform float particleDensity;', // Uniform to control particle density
        'varying vec2 vUv;',
        'float random(vec2 co) {',
        '    return fract(sin(dot(co.xy, vec2(12.9898, 78.233))) * 43758.5453);',
        '}',
        'void main() {',
        // Generate a random value based on the fragment coordinates
        '    float rand = random(gl_FragCoord.xy);',
        // Discard the pixel based on the random value and particle density
        '    if (rand > particleDensity) discard;',
        // Introduce noise to create roughness
        '    float noise = random(vUv * 100.0) * noiseIntensity;',
        '    vec4 color = vec4(vec3(noise), 1.0);', // Dust particles color
        '    gl_FragColor = color;',
        '}'
    ].join('\n')
}

function uniformsRingPixel(colorScheme) {
    return {
        color: colorScheme,
        discardProbability: { value: 0.1 }, // Adjust this value to control the probability
        noiseIntensity: { value: 0.5 }, // Adjust this value to control the noise intensity
        particleDensity: { value: 0.5 } // Adjust this value to control the particle density
    };
}

export { vertexShaderRingTexture, fragShaderRingTexture, uniformsRingTexture, vertexShaderRingPixel, fragShaderRingPixel, uniformsRingPixel };