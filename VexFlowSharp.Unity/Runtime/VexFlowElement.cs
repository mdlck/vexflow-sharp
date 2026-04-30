using System.Collections.Generic;
using UnityEngine.UIElements;
using VexFlowSharp.Api;

namespace VexFlowSharp.Unity
{
    /// <summary>
    /// Custom VisualElement that renders a VexFlowSharp score via Painter2D.
    ///
    /// Usage:
    ///   1. Create VexFlowElement and add to the UI hierarchy.
    ///   2. Pre-allocate labels with PreAllocateLabels(n) if text annotations are expected.
    ///   3. Build a Factory using vfElement.Context as the render context.
    ///   4. Call Render(factory) to trigger a repaint.
    ///
    /// The long-lived UIElementsRenderContext is updated with the frame's Painter2D
    /// inside the generateVisualContent callback, so Factory only needs to be
    /// constructed once (or rebuilt when the score changes).
    /// </summary>
    public class VexFlowElement : VisualElement
    {
        // ── Fields ─────────────────────────────────────────────────────────────

        private Factory _factory;
        private readonly UIElementsRenderContext _context;
        private readonly List<Label> _labelPool = new List<Label>();
        private int _activeLabelCount;

        // ── Constructor ────────────────────────────────────────────────────────

        public VexFlowElement()
        {
            _context = new UIElementsRenderContext(this);
            generateVisualContent += OnGenerateVisualContent;
        }

        // ── Public API ─────────────────────────────────────────────────────────

        /// <summary>
        /// Long-lived render context backed by this element's Painter2D.
        /// Pass this to the Factory constructor:
        ///   var factory = new Factory(vfElement.Context, width, height);
        /// </summary>
        public UIElementsRenderContext Context => _context;

        /// <summary>
        /// Triggers a repaint of the element using the provided Factory.
        /// Call this whenever the score data changes.
        /// The Factory must have been constructed with this element's Context property.
        /// </summary>
        public void Render(Factory factory)
        {
            _factory = factory;
            // Reset active label count; labels are recycled from the pool each repaint
            _activeLabelCount = 0;
            MarkDirtyRepaint();
        }

        /// <summary>
        /// Pre-allocate pooled Labels so that no VisualElement.Add() calls are
        /// needed during the generateVisualContent callback (which can trigger
        /// repaint loops per RESEARCH.md Pitfall 3).
        ///
        /// Call before the first Render() with an estimate of the maximum number
        /// of text annotations the score will produce. 32 is a safe default.
        /// </summary>
        public void PreAllocateLabels(int count)
        {
            for (int i = _labelPool.Count; i < count; i++)
            {
                var label = new Label();
                label.style.position = Position.Absolute;
                label.style.display = DisplayStyle.None;
                Add(label);
                _labelPool.Add(label);
            }
        }

        /// <summary>
        /// Called by UIElementsRenderContext.FillText to get or create a pooled Label.
        /// Labels are recycled to avoid VisualElement mutations during the
        /// generateVisualContent callback where possible.
        /// </summary>
        internal Label GetOrCreateLabel()
        {
            if (_activeLabelCount < _labelPool.Count)
            {
                var existing = _labelPool[_activeLabelCount++];
                existing.style.display = DisplayStyle.Flex;
                return existing;
            }
            var label = new Label();
            label.style.position = Position.Absolute;
            Add(label);
            _labelPool.Add(label);
            _activeLabelCount++;
            return label;
        }

        // ── generateVisualContent callback ─────────────────────────────────────

        private void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (_factory == null) return;

            // Inject the current frame's Painter2D into the long-lived context
            _context.SetPainter(mgc.painter2D);
            _factory.Draw();
            // Clear the Painter2D reference so stale access outside the callback is
            // caught as a null-reference rather than silently drawing into a dead painter
            _context.SetPainter(null);

            // Hide unused pooled labels from previous render cycle
            for (int i = _activeLabelCount; i < _labelPool.Count; i++)
                _labelPool[i].style.display = DisplayStyle.None;
        }
    }
}
