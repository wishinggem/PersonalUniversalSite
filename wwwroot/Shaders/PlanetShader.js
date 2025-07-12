function vertexShader() {
    return [
        'varying vec2 vUv;',
        'varying float vHeight;',
        'uniform sampler2D noiseTexture;',
        'uniform float noiseIntensity;',
        'uniform float maxRadius;',
        'void main() {',
        '    vUv = uv;',
        '    float height = texture2D(noiseTexture, uv).r * noiseIntensity;',
        '    // Apply a smoothing function to the height',
        '    height = smoothstep(0.0, 1.0, height);',
        '    vec3 newPosition = position + normal * height;',
        '    float distanceFromCenter = length(newPosition);',
        '    if (distanceFromCenter > maxRadius) {',
        '        newPosition = normalize(newPosition) * maxRadius;',
        '    }',
        '    vHeight = height;',
        '    gl_Position = projectionMatrix * modelViewMatrix * vec4(newPosition, 1.0);',
        '}'
    ].join('\n');
}

function fragmentShader() {
    return [
        'varying vec2 vUv;',
        'varying float vHeight;',
        'uniform sampler2D mountainTexture;',
        'uniform sampler2D waterTexture;',
        'uniform sampler2D grassTexture;',
        'uniform float maxRadius;',
        'uniform float waterMountainRatio;',
        'void main() {',
        '    float normalizedHeight = clamp(vHeight / maxRadius, 0.0, 1.0);',
        '    vec4 waterColor = texture2D(waterTexture, vUv);',
        '    vec4 grassColor = texture2D(grassTexture, vUv);',
        '    vec4 mountainColor = texture2D(mountainTexture, vUv);',
        '    vec4 color;',
        '    if (normalizedHeight < waterMountainRatio) {',
        '        color = waterColor;',
        '    } else if (normalizedHeight < waterMountainRatio + 0.3) {', // Adjust the range for grass
        '        float blendFactor = (normalizedHeight - waterMountainRatio) / 0.3;',
        '        color = mix(waterColor, grassColor, blendFactor);',
        '    } else {',
        '        float blendFactor = (normalizedHeight - (waterMountainRatio + 0.3)) / (1.0 - (waterMountainRatio + 0.3));',
        '        color = mix(grassColor, mountainColor, blendFactor);',
        '    }',
        '    gl_FragColor = vec4(color.rgb, 1.0);',
        '}'
    ].join('\n');
}

const textureLoader = new THREE.TextureLoader();
const planetNoise = textureLoader.load('/Textures/planetNoise.png');
const mountainTexture = textureLoader.load('/Textures/mountain.jpg');
const waterTexture = textureLoader.load('/Textures/water.jpg');
const grassTexture = textureLoader.load('/Textures/grass.jpg');

function uniforms(intensity, maxRadius, waterMountainRatio, hueColor) {
    return {
        noiseTexture: { type: 't', value: planetNoise },
        mountainTexture: { type: 't', value: mountainTexture },
        waterTexture: { type: 't', value: waterTexture },
        grassTexture: { type: 't', value: grassTexture },
        noiseIntensity: { type: 'f', value: intensity },
        maxRadius: { type: 'f', value: maxRadius },
        waterMountainRatio: { type: 'f', value: waterMountainRatio },
        hueColor: { type: 'v3', value: new THREE.Color(hueColor) }
    };
}

export { vertexShader, fragmentShader, uniforms };