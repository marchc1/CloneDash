#version 330

struct Bone{
	mat4 local;
	mat4 final;
};

layout(location = 0) in vec3 pos;
layout(location = 1) in vec2 tex;
layout(location = 2) in vec3 norm;
layout(location = 3) in vec4 vertexColor;
layout(location = 4) in vec4 boneIds;
layout(location = 5) in vec4 weights;

out vec2 fragTexCoord;
out vec4 fragColor;

const int MAX_BONES = 50;
const int MAX_BONE_INFLUENCE = 4;

uniform Bone bones[MAX_BONES];
uniform mat4 mvp;

void main()
{
    mat4 bone_transform = mat4(0.0);
	fragColor = vertexColor;
	
    for(int i = 0 ; i < MAX_BONE_INFLUENCE; i++)
    {
		float weight = weights[i];
		if(weight == 0){ continue; }
		
		int boneID = int(boneIds[i]);
		
		//if(boneIds[i] == 0){
			//fragColor = vec4(1.0, 0.5, 0.5, 1.0);
		//}

        if(boneID >= MAX_BONES) 
        {
			fragColor = vec4(boneIds.x / 2555255555.0, boneIds.y, boneIds.z, 1.0);
			break;
        }
		
		Bone bone = bones[boneID];
		
		bone_transform += bone.final * weight;
    }
	
	vec3 finalPos = vec3(mvp * bone_transform * vec4(pos, 1.0));
	
    gl_Position = vec4(finalPos, 1.0f);
    fragTexCoord = tex;
}