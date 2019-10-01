using UnityEngine;

namespace TEDCore.HiZ
{
    [RequireComponent(typeof(HiZBuffer))]
    public class Viewer : MonoBehaviour
    {
        [Range(0, 16)]
        public int lOD = 0;

        private Shader m_shader;
        public Shader shader
        {
            get
            {
                if (m_shader == null)
                {
                    m_shader = Shader.Find("Hidden/Viewer");
                }

                return m_shader;
            }
        }

        private Material m_material;
        public Material material
        {
            get
            {
                if (m_material == null)
                {
                    if (shader == null || !shader.isSupported)
                    {
                        return null;
                    }

                    m_material = new Material(shader);
                }

                return m_material;
            }
        }

        private RenderTexture m_hiZBuffer
        {
            get
            {
                HiZBuffer hiZBuffer = GetComponent<HiZBuffer>();

                if (hiZBuffer == null)
                {
                    return null;
                }

                return hiZBuffer.hiZBuffer;
            }
        }

        public void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (m_hiZBuffer == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            material.SetInt("_LOD", lOD);
            Graphics.Blit(m_hiZBuffer, destination, material);
        }
    }
}