# FaraMultiVrmConverter

This is a Unity Editor extension tool for batch converting VRChat avatars to VRM format. It supports processing multiple avatars at once and includes features such as copying components from existing VRMs and automatic thumbnail generation.

## Key Features

- **Batch Conversion**: It is possible to convert multiple VRChat avatar Prefabs to VRM at once.
- **Component Copying**: VRM components such as BlendShapeProxy and Meta information can be copied from an existing VRM.
- **Automatic Thumbnail Generation**: Automatically generate avatar thumbnails at a specified resolution and set them in VRM Meta.
- **Automatic Object Deletion**: It is possible to set to automatically delete unnecessary objects (such as objects with VRChat specific components) when converting to VRM.
- **Multi-language Support**: Japanese and English display can be toggled.

## Requirements

- Unity 2022.3
- VRM 0.x (UniVRM)
- UniVRM-Extensions
- NDMF (Non-Destructive Modular Framework)
- VRChat SDK - Avatars

## How to Use

1. Open the window from `FaraScripts/VRMMultiConverter`.
2. Drag and drop the VRChat avatar Prefabs you want to convert into the "VRC -> VRM Conversion avatar" list.
3. If necessary, check "Copy VRM Components from another avatar" and specify the base VRM Prefab.
4. Set the VRM output path, Meta information (Version, Author), and thumbnail save path.
5. If there are objects you want to delete, open the settings file and add the object names.
6. Click the "Convert VRC -> VRM" button to execute.

## License

[MIT License](LICENSE)
