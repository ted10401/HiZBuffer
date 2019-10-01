using UnityEngine;
using UnityEngine.Rendering;

namespace TEDCore.HiZ
{
    [RequireComponent(typeof(Camera))]
    public class HiZBuffer : MonoBehaviour
    {
        private const int MAXIMUM_BUFFER_SIZE = 2048;

        private enum Pass
        {
            Blit,
            Reduce
        }

        private Shader m_shader;
        public Shader shader
        {
            get
            {
                if (m_shader == null)
                {
                    m_shader = Shader.Find("Hidden/Hi-Z Buffer");
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

        private Camera m_camera;
        public new Camera camera
        {
            get
            {
                if (m_camera == null)
                {
                    m_camera = GetComponent<Camera>();
                }

                return m_camera;
            }
        }

        public RenderTexture hiZBuffer { get; private set; }

        private int m_lODCount;
        public int lODCount
        {
            get
            {
                if (hiZBuffer == null)
                {
                    return 0;
                }

                return 1 + m_lODCount;
            }
        }

        private CommandBuffer m_commandBuffer;
        private CameraEvent m_cameraEvent = CameraEvent.BeforeReflections;

        private int[] m_Temporaries;

        private void OnEnable()
        {
            camera.depthTextureMode = DepthTextureMode.Depth;
        }

        private void OnDisable()
        {
            if (camera != null)
            {
                if (m_commandBuffer != null)
                {
                    camera.RemoveCommandBuffer(m_cameraEvent, m_commandBuffer);
                    m_commandBuffer = null;
                }
            }

            ReleaseBuffer();
        }

        private void ReleaseBuffer()
        {
            if (hiZBuffer != null)
            {
                hiZBuffer.Release();
                hiZBuffer = null;
            }
        }

        void OnPreRender()
        {
            int size = Mathf.Max(camera.pixelWidth, camera.pixelHeight);
            size = Mathf.Min(Mathf.NextPowerOfTwo(size), MAXIMUM_BUFFER_SIZE);

            m_lODCount = (int)Mathf.Floor(Mathf.Log(size, 2f));

            bool isCommandBufferInvalid = false;

            if (m_lODCount == 0)
            {
                return;
            }

            if (hiZBuffer == null || hiZBuffer.width != size || hiZBuffer.height != size)
            {
                ReleaseBuffer();

                hiZBuffer = new RenderTexture(size, size, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
                hiZBuffer.filterMode = FilterMode.Point;

                hiZBuffer.useMipMap = true;
                hiZBuffer.autoGenerateMips = false;

                hiZBuffer.Create();

                hiZBuffer.hideFlags = HideFlags.HideAndDontSave;

                isCommandBufferInvalid = true;
            }

            if (m_commandBuffer == null || isCommandBufferInvalid == true)
            {
                m_Temporaries = new int[m_lODCount];

                if (m_commandBuffer != null)
                {
                    camera.RemoveCommandBuffer(m_cameraEvent, m_commandBuffer);
                }

                m_commandBuffer = new CommandBuffer();
                m_commandBuffer.name = "Hi-Z Buffer";

                RenderTargetIdentifier id = new RenderTargetIdentifier(hiZBuffer);

                m_commandBuffer.Blit(null, id, material, (int)Pass.Blit);

                for (int i = 0; i < m_lODCount; ++i)
                {
                    m_Temporaries[i] = Shader.PropertyToID("_09659d57_Temporaries" + i.ToString());

                    size >>= 1;

                    if (size == 0)
                    {
                        size = 1;
                    }

                    m_commandBuffer.GetTemporaryRT(m_Temporaries[i], size, size, 0, FilterMode.Point, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);

                    if (i == 0)
                    {
                        m_commandBuffer.Blit(id, m_Temporaries[0], material, (int)Pass.Reduce);
                    }
                    else
                    {
                        m_commandBuffer.Blit(m_Temporaries[i - 1], m_Temporaries[i], material, (int)Pass.Reduce);
                    }

                    m_commandBuffer.CopyTexture(m_Temporaries[i], 0, 0, id, 0, i + 1);

                    if (i >= 1)
                    {
                        m_commandBuffer.ReleaseTemporaryRT(m_Temporaries[i - 1]);
                    }
                }

                m_commandBuffer.ReleaseTemporaryRT(m_Temporaries[m_lODCount - 1]);

                camera.AddCommandBuffer(m_cameraEvent, m_commandBuffer);
            }
        }
    }
}