function getRandomInteger(min, max) {
    return Math.floor(Math.random() * (max - min + 1)) + min;
}

function getRandomFloat(min, max) {
    return Math.random() * (max - min) + min;
}

function getRandomHex() {
    // Generate a random integer between 0 and 255
    const randomInt = Math.floor(Math.random() * 256);
    // Convert the integer to a hex string and pad with leading zero if necessary
    const hexString = randomInt.toString(16).padStart(2, '0');
    return hexString;
}

// Example usage to generate a random hex color
function getRandomHexColor() {
    const red = getRandomHex();
    const green = getRandomHex();
    const blue = getRandomHex();
    return `0x${red}${green}${blue}`;
}

function Vector3Distance(vector1, vector2) {
    return Math.abs(Math.sqrt(vector1.x * vector1.x, vector1.y * vector1.y, vector1.z * vector1.z) - Math.sqrt(vector2.x * vector2.x, vector2.y * vector2.y, vector2.z * vector2.z))
}

export { getRandomInteger, getRandomFloat, getRandomHexColor, Vector3Distance };