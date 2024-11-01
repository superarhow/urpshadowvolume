# URP Shadow Volume
A shadow volume implementation (ZP- Stencil Shadow Volume) under URP in Unity. (2021.3)

# Brief
I implemented unique shadow mapping in previous projects but I'm trying to study other algorithms.

# Features
![image](https://github.com/user-attachments/assets/d80acba8-5201-449b-83bb-fae3998af6f0)

## Global Mode
All opaque objects use shadow volume. (Please disable the origin shadow)
## Object Mode
Objects attached with script "ZPStencilShadowObject" use shadow volume.

# Installation

1. Clone the repository
2. Open it using Unity

To embed into other URP projects, 

1. Copy the ZPStencilShadow folder to any place in the URP project.
2. Add the "ZPStencilShadowRenderFeature" to the URP render pipeline settings.
3. Be sure to assign the material "ZPStencilShadow.mat" to the new render feature (or black screen will happen)

# TODO
* Reduce drawcalls
* Edges of faces

# License
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

# Contact
superarhow (superarhow@hotmail.com)



