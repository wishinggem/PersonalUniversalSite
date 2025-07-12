function renderStandardModel(loader, scene, url, position, scale) {
    return new Promise((resolve, reject) => {
        loader.load(url, function (gltf) {
            var model = gltf.scene;
            model.position.copy(position);

            if (scale !== null) {
                model.scale.copy(new THREE.Vector3(scale, scale, scale));
            }

            scene.add(model);
            resolve(model);
        }, undefined, function (error) {
            reject(error);
        });
    });
}

export { renderStandardModel };