using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Renderer Feature que logra el "color selectivo" (efecto Sin City) sin una
/// segunda cámara. Funciona en una sola pasada usando el stencil buffer:
///
///   1. Pass "stencil": dibuja los objetos de la capa "Resaltado" escribiendo
///      stencil = 1 (se dibujan a color, con profundidad correcta).
///   2. Pass "desaturar": blit a pantalla completa que convierte a blanco y
///      negro (con tinte azulado) TODO salvo lo marcado con stencil = 1.
///
/// Así no hay re-renderizado "por encima" ni problemas de profundidad en VR.
/// Este feature se agrega al renderer URP desde el asset (ver m_RendererFeatures).
/// </summary>
public class SelectiveDesaturationFeature : ScriptableRendererFeature
{
    /// <summary>Flag estático que activa/desactiva el efecto (lo maneja el tutorial).</summary>
    public static bool Activo = false;

    public LayerMask capaResaltado = 1 << 8; // capa "Resaltado" (TagManager)
    public Material materialDesaturar;        // shader "Custom/DesaturateStencil"

    private PassStencil passStencil;
    private PassDesaturar passDesaturar;

    public override void Create()
    {
        // Si no se asignó material en el Inspector, se crea acá con el shader.
        if (materialDesaturar == null)
            materialDesaturar = new Material(Shader.Find("Custom/DesaturateStencil"));

        passStencil = new PassStencil(capaResaltado);
        passStencil.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques;

        passDesaturar = new PassDesaturar(materialDesaturar);
        passDesaturar.renderPassEvent = RenderPassEvent.AfterRendering;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!Activo)
            return;

        renderer.EnqueuePass(passStencil);
        renderer.EnqueuePass(passDesaturar);
    }

    // ------------------------------------------------------------------
    // Pass 1: escribe stencil = 1 en los objetos de la capa "Resaltado"
    // ------------------------------------------------------------------
    class PassStencil : ScriptableRenderPass
    {
        private readonly LayerMask mask;
        private readonly List<ShaderTagId> tags;
        private FilteringSettings filtering;
        private RenderStateBlock block;

        public PassStencil(LayerMask m)
        {
            mask = m;
            tags = new List<ShaderTagId>
            {
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("LightweightForward")
            };
            filtering = new FilteringSettings(RenderQueueRange.all, mask);

            // stencil: escribir 1 donde se dibuja.
            var stencil = new StencilState(
                true,                    // enabled
                1,                       // readMask
                1,                       // writeMask
                CompareFunction.Always,  // compareFunction (cara frontal)
                StencilOp.Replace,       // passOperation
                StencilOp.Keep,          // failOperation
                StencilOp.Keep,          // zFailOperation
                CompareFunction.Always,  // compareFunctionBack (cara trasera)
                StencilOp.Replace,       // passOperationBack
                StencilOp.Keep,          // failOperationBack
                StencilOp.Keep           // zFailOperationBack
            );
            block = new RenderStateBlock(RenderStateMask.Stencil);
            block.stencilState = stencil;
            block.stencilReference = 1; // el valor que se escribe en el stencil
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var drawSettings = CreateDrawingSettings(tags, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filtering, ref block);
        }
    }

    // ------------------------------------------------------------------
    // Pass 2: desatura la pantalla salvo lo marcado con stencil = 1
    // ------------------------------------------------------------------
    class PassDesaturar : ScriptableRenderPass
    {
        private readonly Material mat;
        private readonly int idTemp = Shader.PropertyToID("_TempDesaturate");

        public PassDesaturar(Material m)
        {
            mat = m;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (mat == null)
                return;

            var cmd = CommandBufferPool.Get("DesaturateStencil");
            var source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            cmd.GetTemporaryRT(idTemp, renderingData.cameraData.cameraTargetDescriptor);
            cmd.Blit(source.nameID, idTemp, mat);
            cmd.Blit(idTemp, source.nameID);
            cmd.ReleaseTemporaryRT(idTemp);

            context.ExecuteCommandBuffer(cmd);
            context.Submit();
            CommandBufferPool.Release(cmd);
        }
    }
}
