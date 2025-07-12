function vertexShaderAtmo() {
    return [
        'varying vec3 vNormal;',
        'varying vec3 vPosition;',
        'void main() {',
        'vNormal = normalize(normalMatrix * normal);',
        'vPosition = (modelViewMatrix * vec4(position, 1.0)).xyz;',
        'gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);',
        '}'
    ].join('\n');
}

function fragShaderAtmo() {
    return [
        'precision highp float;',
        'uniform vec3 atmosphereColor;',
        'uniform vec3 objectColor;',
        'uniform vec3 ambientLightColor;',
        'varying vec3 vNormal;',
        'varying vec3 vPosition;',
        'void main() {',
        'vec3 lightPosition = vec3(-10.0, 10.0, 0.0);',
        'vec3 lightDirection = normalize(lightPosition - vPosition);',
        'float dotNL = max(dot(lightDirection, vNormal), 0.0);',
        'float intensity = smoothstep(0.2, 1.0, dotNL);',
        'intensity *= 1.0;',
        'vec3 atmosphere = atmosphereColor * intensity;',
        'vec3 ambient = ambientLightColor * 0.3;',

        // Approximate subsurface scattering
        'vec3 viewDirection = normalize(-vPosition);',
        'float scatterFactor = dot(viewDirection, vNormal) * 0.4 + 0.4;', // Reduce scatter factor
        'vec3 scatterColor = atmosphereColor * scatterFactor;',
        'vec3 finalColor = mix(objectColor, scatterColor + ambient, 0.6);', // Reduce blend factor

        'gl_FragColor = vec4(finalColor, 1.0);',
        '}'
    ].join('\n');
}

function uniformsAtmo(color) {
    return {
        atmosphereColor: { type: 'vec3', value: new THREE.Color(0x74ebd5) },
        objectColor: { type: 'vec3', value: color },
        //ambientLightColor: { type: 'vec3', value: new THREE.Color(0x404040) } // Add ambient light color
    };
}

export { vertexShaderAtmo, fragShaderAtmo, uniformsAtmo };