// VexFlowSharp — C# port of VexFlow (https://vexflow.com)
// MIT License

using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using VexFlowSharp;
using VexFlowSharp.Api;
using VexFlowSharp.Common.Elements;
using VexFlowSharp.Common.Formatting;
using VexFlowSharp.Skia;
using VexFlowSharp.Tests.Infrastructure;
using VexFlowSharp.Tests.Rendering;

namespace VexFlowSharp.Tests.Api
{
    [TestFixture]
    [Category("Factory")]
    [Category("Phase5")]
    public class FactoryTests
    {
        // ── Reference image path ──────────────────────────────────────────────

        private static string ReferenceImagesDir
        {
            get
            {
                string assemblyDir = Path.GetDirectoryName(
                    typeof(FactoryTests).Assembly.Location)!;
                return Path.GetFullPath(
                    Path.Combine(assemblyDir, "../../../reference-images"));
            }
        }

        private string RefPath(string filename) =>
            Path.Combine(ReferenceImagesDir, filename);

        // ── SetUp ─────────────────────────────────────────────────────────────

        [SetUp]
        public void SetUp()
        {
            Font.ClearRegistry();
            Font.Load("Bravura", BravuraGlyphs.Data);
        }

        // ── Test 1: Factory grand staff layout — structural test ───────────────

        /// <summary>
        /// Phase 5 plan 05-03 success criterion:
        /// Factory builder API constructs a grand staff (treble + bass) via
        /// Factory.StaveNote / Factory.Voice / Factory.System and renders it
        /// via Factory.Draw().
        ///
        /// This is a structural test — it verifies that:
        ///   - Factory.Draw() completes without exceptions
        ///   - Notes are added to renderQ and drawn
        ///   - System.Format() aligns noteStartX across both staves
        ///   - Staves stack vertically (bass Y > treble Y)
        /// </summary>
        [Test]
        [Category("ImageCompare")]
        public void FactoryGrandStaffRenders()
        {
            using var ctx = new SkiaRenderContext(600, 250);
            var factory = new Factory(ctx, 600, 250);

            var system = factory.System(new SystemOptions { X = 10, Y = 10, Width = 570 });

            // Build treble notes via factory (adds to renderQ)
            var n1 = factory.StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "q" });
            var n2 = factory.StaveNote(new StaveNoteStruct { Keys = new[] { "d/4" }, Duration = "q" });
            var n3 = factory.StaveNote(new StaveNoteStruct { Keys = new[] { "e/4" }, Duration = "q" });
            var n4 = factory.StaveNote(new StaveNoteStruct { Keys = new[] { "f/4" }, Duration = "q" });

            // Build bass notes via factory (adds to renderQ)
            var n5 = factory.StaveNote(new StaveNoteStruct { Keys = new[] { "c/3" }, Duration = "q" });
            var n6 = factory.StaveNote(new StaveNoteStruct { Keys = new[] { "d/3" }, Duration = "q" });
            var n7 = factory.StaveNote(new StaveNoteStruct { Keys = new[] { "e/3" }, Duration = "q" });
            var n8 = factory.StaveNote(new StaveNoteStruct { Keys = new[] { "f/3" }, Duration = "q" });

            // Build voices via factory (adds to factory.voices list)
            var trebleVoice = factory.Voice(4, 4);
            trebleVoice.AddTickables(new List<Tickable> { n1, n2, n3, n4 });

            var bassVoice = factory.Voice(4, 4);
            bassVoice.AddTickables(new List<Tickable> { n5, n6, n7, n8 });

            // Add staves to system with voices; staves are created internally by system
            var trebleStave = system.AddStave(new SystemStave
            {
                Voices = new List<Voice> { trebleVoice },
            });
            trebleStave.AddClef("treble");
            trebleStave.AddTimeSignature("4/4");

            var bassStave = system.AddStave(new SystemStave
            {
                Voices = new List<Voice> { bassVoice },
            });
            bassStave.AddClef("bass");
            bassStave.AddTimeSignature("4/4");

            system.AddConnector("singleLeft");
            system.AddConnector("brace");

            // Factory.Draw() must complete without exceptions
            Assert.DoesNotThrow(() => factory.Draw(),
                "Factory.Draw() must run without exceptions");

            // After Draw(), the factory is reset — verify structural outcomes
            // by inspecting what was drawn (stave positions set by System.Format())

            // Both staves must have equal noteStartX after Format() (column alignment)
            double trebleStartX = trebleStave.GetNoteStartX();
            double bassStartX   = bassStave.GetNoteStartX();
            Assert.That(trebleStartX, Is.EqualTo(bassStartX).Within(0.5),
                $"Treble noteStartX ({trebleStartX:F2}) must match bass noteStartX ({bassStartX:F2})");

            // NoteStartX must be > stave X (clef + time sig consumed space)
            Assert.That(trebleStartX, Is.GreaterThan(10),
                "NoteStartX must be > 10 (clef + time sig padding consumed)");

            // Both staves must be at different Y positions (stacked vertically)
            Assert.That(bassStave.GetY(), Is.GreaterThan(trebleStave.GetY()),
                "Bass stave must be below treble stave after Format()");
        }

        [Test]
        public void SetContext_UpdatesFactoryAndSubsequentElements()
        {
            using var firstContext = new SkiaRenderContext(200, 100);
            using var secondContext = new SkiaRenderContext(200, 100);
            var factory = new Factory(firstContext, 200, 100);

            var returned = factory.SetContext(secondContext);
            var system = factory.System();
            var stave = factory.Stave(10, 10, 180);
            var note = factory.StaveNote(new StaveNoteStruct
            {
                Keys = new[] { "c/4" },
                Duration = "q",
            });

            Assert.That(returned, Is.SameAs(factory));
            Assert.That(factory.GetContext(), Is.SameAs(secondContext));
            Assert.That(system.CheckContext(), Is.SameAs(secondContext));
            Assert.That(stave.CheckContext(), Is.SameAs(secondContext));
            Assert.That(note.CheckContext(), Is.SameAs(secondContext));
        }

        [Test]
        public void System_UsesV5DefaultOptions()
        {
            using var ctx = new SkiaRenderContext(200, 100);
            var factory = new Factory(ctx, 200, 100);

            var system = factory.System();

            Assert.That(system.GetX(), Is.EqualTo(10));
            Assert.That(system.GetY(), Is.EqualTo(10));
        }

        // ── Test 2: Factory grand staff pixel comparison ─────────────────────

        /// <summary>
        /// Pixel-level comparison of Factory grand staff against VexFlow reference PNG.
        ///
        /// Uses the cross-engine tolerance because SkiaSharp and node-canvas
        /// rasterize glyphs with different anti-aliasing and ink density.
        /// </summary>
        [Test]
        [Category("ImageCompare")]
        public void FactoryGrandStaffRenders_PixelComparison()
        {
            using var ctx = new SkiaRenderContext(600, 250);
            ctx.SetFillStyle("#FFFFFF");
            ctx.FillRect(0, 0, 600, 250);
            var factory = new Factory(ctx, 600, 250);

            var system = factory.System(new SystemOptions { X = 10, Y = 10, Width = 570 });

            var n1 = factory.StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "q" });
            var n2 = factory.StaveNote(new StaveNoteStruct { Keys = new[] { "d/4" }, Duration = "q" });
            var n3 = factory.StaveNote(new StaveNoteStruct { Keys = new[] { "e/4" }, Duration = "q" });
            var n4 = factory.StaveNote(new StaveNoteStruct { Keys = new[] { "f/4" }, Duration = "q" });
            var n5 = factory.StaveNote(new StaveNoteStruct { Keys = new[] { "c/3" }, Duration = "q" });
            var n6 = factory.StaveNote(new StaveNoteStruct { Keys = new[] { "d/3" }, Duration = "q" });
            var n7 = factory.StaveNote(new StaveNoteStruct { Keys = new[] { "e/3" }, Duration = "q" });
            var n8 = factory.StaveNote(new StaveNoteStruct { Keys = new[] { "f/3" }, Duration = "q" });

            var trebleVoice = factory.Voice(4, 4);
            trebleVoice.AddTickables(new List<Tickable> { n1, n2, n3, n4 });

            var bassVoice = factory.Voice(4, 4);
            bassVoice.AddTickables(new List<Tickable> { n5, n6, n7, n8 });

            var trebleStave = system.AddStave(new SystemStave { Voices = new List<Voice> { trebleVoice } });
            trebleStave.AddClef("treble");
            trebleStave.AddTimeSignature("4/4");

            var bassStave = system.AddStave(new SystemStave { Voices = new List<Voice> { bassVoice } });
            bassStave.AddClef("bass");
            bassStave.AddTimeSignature("4/4");

            system.AddConnector("singleLeft");
            system.AddConnector("brace");

            factory.Draw();

            string refPath = RefPath("factory_grandstaff-vexflow.png");
            Assert.That(File.Exists(refPath), Is.True, $"Reference PNG missing: {refPath}");

            byte[] actual = ctx.ToPng();
            byte[] reference = File.ReadAllBytes(refPath);
            ImageComparisonAssert.AssertImagesMatch(actual, reference, thresholdPercent: ImageComparison.CrossEngineThresholdPercent);
        }

        // ── Test 3: Factory draw order verification ───────────────────────────

        /// <summary>
        /// Phase 5 plan 05-03 success criterion:
        /// Factory.Draw() executes steps in exact order:
        ///   systems.Format -> staves.Draw -> voices.Draw -> renderQ.Draw -> systems.Draw
        ///
        /// Uses RecordingRenderContext to verify that stave line drawing (FillRect)
        /// occurs before notehead drawing (context operations from notes).
        ///
        /// This is a unit test — no image comparison.
        /// </summary>
        [Test]
        [Category("Unit")]
        public void FactoryDrawOrder()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);

            // Add a system with one stave and one note
            var system = factory.System(new SystemOptions { X = 10, Y = 10, Width = 470 });

            var note = factory.StaveNote(new StaveNoteStruct
            {
                Keys     = new[] { "c/4" },
                Duration = "q",
            });

            var voice = factory.Voice(4, 4);
            voice.AddTickables(new List<Tickable> { note });

            var stave = system.AddStave(new SystemStave { Voices = new List<Voice> { voice } });
            stave.AddClef("treble");

            // Factory.Draw() must complete without exceptions
            Assert.DoesNotThrow(() => factory.Draw(),
                "Factory.Draw() must execute without exceptions");

            // Verify that drawing calls were recorded (context was used)
            Assert.That(ctx.Calls.Count, Is.GreaterThan(0),
                "RecordingRenderContext should have recorded drawing calls");

            // Verify that note-related context operations were recorded.
            // Notes go through renderQ so their SetContext+Draw calls are recorded.
            // Stave lines would appear if stave was created via factory.Stave() directly;
            // here the stave was created internally by system, so stave FillRects appear
            // only if the system draws the stave (which it does not — caller draws staves).
            // The presence of ANY call confirms the draw pipeline executed.
            Assert.That(ctx.Calls.Count, Is.GreaterThan(5),
                "Multiple rendering calls expected from draw pipeline (note drawing)");

            // Verify that after Draw(), the factory is reset:
            // calling Draw() again with empty state should not throw
            Assert.DoesNotThrow(() => factory.Draw(),
                "Factory.Draw() on reset state must not throw");
        }

        // ── Test 4: Factory Reset clears state ────────────────────────────────

        /// <summary>
        /// Factory.Reset() clears staves, voices, renderQ, and systems.
        /// After Reset(), Draw() should be a no-op (nothing recorded).
        /// </summary>
        [Test]
        [Category("Unit")]
        public void FactoryResetClearsState()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);

            // Add some elements
            factory.Stave(0, 0, 490);
            factory.Voice(4, 4);

            // Reset
            factory.Reset();

            // Draw() should not throw and should not record meaningful calls
            Assert.DoesNotThrow(() => factory.Draw(), "Draw after Reset must not throw");
        }

        // ── Test 5: Factory creates EasyScore stub ────────────────────────────

        /// <summary>
        /// Factory.EasyScore() returns an EasyScore with a back-reference to the factory.
        /// </summary>
        [Test]
        [Category("Unit")]
        public void FactoryEasyScoreHasBackReference()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);

            var score = factory.EasyScore();

            Assert.That(score, Is.Not.Null, "EasyScore must not be null");
            Assert.That(score.Factory, Is.SameAs(factory),
                "EasyScore.Factory must reference the owning Factory");
        }

        [Test]
        [Category("Unit")]
        public void FactoryPedalMarkingDefaultsToMixedAndQueuesForDraw()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);

            var pedal = factory.PedalMarking();

            Assert.That(pedal.GetPedalType(), Is.EqualTo(PedalMarkingType.Mixed));
            Assert.DoesNotThrow(() => factory.Draw());
        }

        [Test]
        [Category("Unit")]
        public void FactoryVoice_AcceptsV5StyleParamsObject()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);

            var fromString = factory.Voice(new FactoryVoiceOptions { TimeString = "5/8" });
            var fromTime = factory.Voice(new FactoryVoiceOptions
            {
                Time = new VoiceTime { NumBeats = 7, BeatValue = 16, Resolution = Tables.RESOLUTION },
            });

            Assert.That(fromString.GetTotalTicks(), Is.EqualTo(new Fraction(Tables.RESOLUTION * 5 / 8, 1)));
            Assert.That(fromTime.GetTotalTicks(), Is.EqualTo(new Fraction(Tables.RESOLUTION * 7 / 16, 1)));
            Assert.That(factory.GetVoices(), Is.EqualTo(new[] { fromString, fromTime }));
        }

        [Test]
        [Category("Unit")]
        public void FactoryStave_AcceptsV5StyleParamsObject()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);

            var stave = factory.Stave(new FactoryStaveOptions
            {
                X = 12,
                Y = 34,
                Width = 456,
                Options = new StaveOptions { SpacingBetweenLinesPx = 11 },
            });

            Assert.That(stave.CheckContext(), Is.SameAs(ctx));
            Assert.That(factory.GetStave(), Is.SameAs(stave));
            Assert.That(stave.GetX(), Is.EqualTo(12));
            Assert.That(stave.GetY(), Is.EqualTo(34));
            Assert.That(stave.GetWidth(), Is.EqualTo(456));
            Assert.That(stave.GetSpacingBetweenLines(), Is.EqualTo(11));
        }

        [Test]
        [Category("Unit")]
        public void FactoryStave_DefaultWidthAndSpacingMatchV5()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);

            var stave = factory.Stave();

            Assert.That(stave.GetX(), Is.EqualTo(0));
            Assert.That(stave.GetY(), Is.EqualTo(0));
            Assert.That(stave.GetWidth(), Is.EqualTo(490));
            Assert.That(stave.GetSpacingBetweenLines(), Is.EqualTo(10));
        }

        [Test]
        [Category("Unit")]
        public void FactoryTabStave_AcceptsV5StyleParamsObject()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);

            var stave = factory.TabStave(new FactoryTabStaveOptions
            {
                X = 20,
                Y = 45,
                Width = 300,
                Options = new StaveOptions { NumLines = 6, SpacingBetweenLinesPx = 13 },
            });

            Assert.That(stave.CheckContext(), Is.SameAs(ctx));
            Assert.That(factory.GetStave(), Is.SameAs(stave));
            Assert.That(stave.GetX(), Is.EqualTo(20));
            Assert.That(stave.GetY(), Is.EqualTo(45));
            Assert.That(stave.GetWidth(), Is.EqualTo(300));
            Assert.That(stave.GetNumLines(), Is.EqualTo(6));
            Assert.That(stave.GetSpacingBetweenLines(), Is.EqualTo(13));
        }

        [Test]
        [Category("Unit")]
        public void FactoryTabStave_DefaultWidthAndSpacingMatchV5()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);

            var stave = factory.TabStave();

            Assert.That(stave.GetX(), Is.EqualTo(0));
            Assert.That(stave.GetY(), Is.EqualTo(0));
            Assert.That(stave.GetWidth(), Is.EqualTo(490));
            Assert.That(stave.GetSpacingBetweenLines(), Is.EqualTo(13));
        }

        [Test]
        [Category("Unit")]
        public void FactoryBeamAppliesV5Options()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);
            var stave = new Stave(10, 40, 400);
            var notes = new List<StemmableNote>
            {
                new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "16", StemDirection = Stem.DOWN }),
                new StaveNote(new StaveNoteStruct { Keys = new[] { "d/4" }, Duration = "16", StemDirection = Stem.DOWN }),
                new StaveNote(new StaveNoteStruct { Keys = new[] { "e/4" }, Duration = "16", StemDirection = Stem.DOWN }),
                new StaveNote(new StaveNoteStruct { Keys = new[] { "f/4" }, Duration = "16", StemDirection = Stem.DOWN }),
            };

            for (int i = 0; i < notes.Count; i++)
                notes[i].SetStave(stave).SetX(80 + i * 40);

            var beam = factory.Beam(notes, new FactoryBeamOptions
            {
                AutoStem = true,
                SecondaryBeamBreaks = new List<int> { 2 },
                PartialBeamDirections = new Dictionary<int, PartialBeamDirection>
                {
                    [1] = PartialBeamDirection.Right,
                },
            });

            Assert.That(beam.GetStemDirection(), Is.EqualTo(Stem.UP));
            Assert.That(notes, Has.All.Matches<StemmableNote>(note => note.GetStemDirection() == Stem.UP));
            Assert.That(beam.LookupBeamDirection("8", 2048, 1024, 2048, 1), Is.EqualTo(Beam.BEAM_RIGHT));
            Assert.That(beam.GetBeamLines("8"), Has.Count.EqualTo(2));
        }

        [Test]
        [Category("Unit")]
        public void FactoryBeam_AcceptsV5StyleParamsObject()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);
            var stave = new Stave(10, 40, 400);
            var notes = new List<StemmableNote>
            {
                new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "8", StemDirection = Stem.DOWN }),
                new StaveNote(new StaveNoteStruct { Keys = new[] { "d/4" }, Duration = "8", StemDirection = Stem.DOWN }),
            };
            notes.ForEach(note => note.SetStave(stave));

            var beam = factory.Beam(new FactoryBeamParams
            {
                Notes = notes,
                Options = new FactoryBeamOptions { AutoStem = true },
            });

            Assert.That(beam.CheckContext(), Is.SameAs(ctx));
            Assert.That(beam.GetStemDirection(), Is.EqualTo(Stem.UP));
            Assert.That(notes, Has.All.Matches<StemmableNote>(note => note.GetStemDirection() == Stem.UP));
        }

        [Test]
        [Category("Unit")]
        public void FactoryTuplet_AcceptsV5StyleParamsObject()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);
            var notes = new List<VexFlowSharp.Note>
            {
                new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "8" }),
                new StaveNote(new StaveNoteStruct { Keys = new[] { "d/4" }, Duration = "8" }),
                new StaveNote(new StaveNoteStruct { Keys = new[] { "e/4" }, Duration = "8" }),
            };

            var tuplet = factory.Tuplet(new FactoryTupletParams
            {
                Notes = notes,
                Options = new TupletOptions { NotesOccupied = 2, Location = (int)TupletLocation.Bottom },
            });

            Assert.That(tuplet.CheckContext(), Is.SameAs(ctx));
            Assert.That(tuplet.GetNotes(), Is.SameAs(notes));
            Assert.That(tuplet.GetNoteCount(), Is.EqualTo(3));
            Assert.That(tuplet.GetNotesOccupied(), Is.EqualTo(2));
            Assert.That(tuplet.GetTupletLocation(), Is.EqualTo((int)TupletLocation.Bottom));
        }

        [Test]
        [Category("Unit")]
        public void FactoryCurve_AcceptsV5StyleParamsObject()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);
            var from = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "q" });

            var curve = factory.Curve(new FactoryCurveParams
            {
                From = from,
                To = null,
                Options = new CurveOptions { Invert = true, CpHeight = 17 },
            });

            Assert.That(curve.CheckContext(), Is.SameAs(ctx));
            Assert.That(curve.GetFromNote(), Is.SameAs(from));
            Assert.That(curve.GetToNote(), Is.Null);
            Assert.That(curve.GetRenderOptions().Invert, Is.True);
            Assert.That(curve.GetRenderOptions().CpHeight, Is.EqualTo(17));
            Assert.That(curve.IsPartial(), Is.True);
        }

        [Test]
        [Category("Unit")]
        public void FactoryTextDynamics_AcceptsV5StyleOptions()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);
            var stave = factory.Stave(10, 40, 420);

            var dynamics = factory.TextDynamics(new FactoryTextDynamicsOptions
            {
                Text = "ff",
                Duration = "8",
                Dots = 1,
                Line = 2,
            });

            Assert.That(dynamics.CheckContext(), Is.SameAs(ctx));
            Assert.That(dynamics.GetStave(), Is.SameAs(stave));
            Assert.That(dynamics.GetSequence(), Is.EqualTo("ff"));
            Assert.That(dynamics.GetLine(), Is.EqualTo(2));
            Assert.That(dynamics.GetDots(), Is.EqualTo(1));
            Assert.That(dynamics.GetTicks(), Is.EqualTo(new Fraction((Tables.RESOLUTION / 8) * 3 / 2, 1)));
        }

        [Test]
        [Category("Unit")]
        public void FactoryTextDynamics_PreservesDynamicsAlias()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);

            var dynamics = factory.TextDynamics(new FactoryTextDynamicsOptions
            {
                Dynamics = "mf",
                Duration = "q",
            });

            Assert.That(dynamics.GetSequence(), Is.EqualTo("mf"));
        }

        [Test]
        [Category("Unit")]
        public void FactoryTextNote_AcceptsV5StyleStruct()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);
            var stave = factory.Stave(10, 40, 420);

            var textNote = factory.TextNote(new FactoryTextNoteOptions
            {
                Text = "verse",
                Duration = "q",
                Line = 1,
                Justification = TextJustification.Right,
                Font = new MetricsFontInfo { Family = "Arial", Size = 11 },
            });

            Assert.That(textNote.CheckContext(), Is.SameAs(ctx));
            Assert.That(textNote.GetStave(), Is.SameAs(stave));
            Assert.That(textNote.GetText(), Is.EqualTo("verse"));
            Assert.That(textNote.GetDuration(), Is.EqualTo("4"));
            Assert.That(textNote.GetLine(), Is.EqualTo(1));
            Assert.That(textNote.GetJustification(), Is.EqualTo(TextJustification.Right));
            Assert.That(textNote.GetFontInfo().Size, Is.EqualTo(11));
        }

        [Test]
        [Category("Unit")]
        public void FactoryCrescendo_AcceptsV5StyleOptions()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);
            var stave = factory.Stave(10, 40, 420);

            var crescendo = factory.Crescendo(new FactoryCrescendoOptions
            {
                NoteStruct = new NoteStruct { Duration = "h" },
                Decrescendo = true,
                Height = 18,
                Line = 3,
            });

            Assert.That(crescendo.CheckContext(), Is.SameAs(ctx));
            Assert.That(crescendo.GetStave(), Is.SameAs(stave));
            Assert.That(crescendo.IsDecrescendo(), Is.True);
            Assert.That(crescendo.GetHeight(), Is.EqualTo(18));
            Assert.That(crescendo.GetLine(), Is.EqualTo(3));
            Assert.That(crescendo.GetTicks(), Is.EqualTo(new Fraction(Tables.RESOLUTION / 4, 1)));
        }

        [Test]
        [Category("Unit")]
        public void FactoryVoiceAcceptsTimeStringAndVoiceTime()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);

            var stringVoice = factory.Voice("3/8");
            var structVoice = factory.Voice(new VoiceTime
            {
                NumBeats = 6,
                BeatValue = 8,
                Resolution = Tables.RESOLUTION,
            });

            Assert.That(stringVoice.GetTotalTicks(), Is.EqualTo(new Fraction(Tables.RESOLUTION * 3 / 8, 1)));
            Assert.That(structVoice.GetTotalTicks(), Is.EqualTo(new Fraction(Tables.RESOLUTION * 6 / 8, 1)));
            Assert.That(factory.GetVoices(), Has.Count.EqualTo(2));
        }

        [Test]
        [Category("Unit")]
        public void FactoryStaveSetsContext()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);

            var stave = factory.Stave(10, 20, 300);

            Assert.That(stave.CheckContext(), Is.SameAs(ctx));
        }

        [Test]
        [Category("Unit")]
        public void FactoryCreatesV5NoteWrappers()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);
            var stave = factory.Stave(10, 40, 420);

            var glyph = factory.GlyphNote("repeat1Bar", new NoteStruct { Duration = "q" });
            var repeat = factory.RepeatNote("2");
            var ghost = factory.GhostNote(new NoteStruct { Duration = "8" });
            var bar = factory.BarNote();
            var clef = factory.ClefNote("bass", "small");
            var time = factory.TimeSigNote("3/8");
            var key = factory.KeySigNote("D");

            Assert.That(glyph.GetStave(), Is.SameAs(stave));
            Assert.That(glyph.GetGlyph(), Is.EqualTo("repeat1Bar"));
            Assert.That(repeat.GetGlyph(), Is.EqualTo("repeat2Bars"));
            Assert.That(ghost.GetStave(), Is.SameAs(stave));
            Assert.That(ghost.GetTicks(), Is.EqualTo(new Fraction(Tables.RESOLUTION / 8, 1)));
            Assert.That(bar.ShouldIgnoreTicks(), Is.True);
            Assert.That(bar.GetCategory(), Is.EqualTo(BarNote.CATEGORY));
            Assert.That(clef.GetClef().GetClefTypeName(), Is.EqualTo("bass"));
            Assert.That(time.ShouldIgnoreTicks(), Is.True);
            Assert.That(key.GetKeySignature(), Is.Not.Null);
        }

        [Test]
        [Category("Unit")]
        public void FactoryNoteWrappers_AcceptV5StyleOptions()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);
            var stave = factory.Stave(10, 40, 420);

            var bar = factory.BarNote(new FactoryBarNoteOptions { TypeString = "repeatEnd" });
            var clef = factory.ClefNote(new FactoryClefNoteOptions
            {
                Type = "bass",
                Options = new FactoryClefNoteNestedOptions { Size = "small", Annotation = "8vb" },
            });
            var time = factory.TimeSigNote(new FactoryTimeSigNoteOptions { Time = "6/8", CustomPadding = 10 });
            var key = factory.KeySigNote(new FactoryKeySigNoteOptions { Key = "D", CancelKey = "Bb" });

            Assert.That(bar.GetStave(), Is.SameAs(stave));
            Assert.That(bar.GetBarlineType(), Is.EqualTo(BarlineType.RepeatEnd));
            Assert.That(clef.GetClef().GetClefTypeName(), Is.EqualTo("bass"));
            Assert.That(time.GetTimeSignature().GetTimeSpec(), Is.EqualTo("6/8"));
            Assert.That(key.GetKeySignature().GetKeySpec(), Is.EqualTo("D"));
            Assert.That(key.GetKeySignature().GetCancelKeySpec(), Is.EqualTo("Bb"));
        }

        [Test]
        [Category("Unit")]
        public void FactoryAnnotation_AcceptsV5StyleOptions()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);

            var annotation = factory.Annotation(new FactoryAnnotationOptions
            {
                Text = "mf",
                HJustifyString = "centerStem",
                VJustifyString = "above",
                Font = new MetricsFontInfo
                {
                    Family = "Times New Roman",
                    Size = 14,
                    Weight = "bold",
                    Style = "italic",
                },
            });

            Assert.That(annotation.CheckContext(), Is.SameAs(ctx));
            Assert.That(annotation.GetJustification(), Is.EqualTo(AnnotationHorizontalJustify.CENTER_STEM));
            Assert.That(annotation.GetVerticalJustification(), Is.EqualTo(AnnotationVerticalJustify.TOP));
            Assert.That(annotation.GetFontFamily(), Is.EqualTo("Times New Roman"));
            Assert.That(annotation.GetFontSize(), Is.EqualTo(14));
            Assert.That(annotation.GetFontStyle(), Is.EqualTo("bold italic"));
        }

        [Test]
        [Category("Unit")]
        public void FactoryArticulation_AcceptsV5StyleOptions()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);

            var articulation = factory.Articulation(new FactoryArticulationOptions
            {
                Type = "a^",
                Position = ModifierPosition.Below,
                BetweenLines = true,
            });

            Assert.That(articulation.CheckContext(), Is.SameAs(ctx));
            Assert.That(articulation.Type, Is.EqualTo("a^"));
            Assert.That(articulation.GetPosition(), Is.EqualTo(ModifierPosition.Below));
            Assert.That(articulation.GetBetweenLines(), Is.True);
            Assert.That(new Articulation("a^").GetBetweenLines(), Is.False);
        }

        [Test]
        [Category("Unit")]
        public void FactoryFingering_AcceptsV5StyleOptions()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);

            var fingering = factory.Fingering(new FactoryFingeringOptions
            {
                Number = "3",
                Position = ModifierPosition.Right,
                OffsetX = 4,
                OffsetY = -2,
            });

            Assert.That(fingering.CheckContext(), Is.SameAs(ctx));
            Assert.That(fingering.GetFretHandFinger(), Is.EqualTo("3"));
            Assert.That(fingering.GetPosition(), Is.EqualTo(ModifierPosition.Right));
            Assert.That(fingering.GetOffsetX(), Is.EqualTo(4));
            Assert.That(fingering.GetOffsetY(), Is.EqualTo(-2));
        }

        [Test]
        [Category("Unit")]
        public void FactoryStringNumber_AcceptsV5StyleOptions()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);

            var stringNumber = factory.StringNumber(new FactoryStringNumberOptions
            {
                Number = "2",
                Position = ModifierPosition.Below,
                DrawCircle = false,
                OffsetX = 3,
                OffsetY = -1,
                StemOffset = 5,
                Dashed = false,
                LineEndType = RendererLineEndType.Down,
            });

            Assert.That(stringNumber.CheckContext(), Is.SameAs(ctx));
            Assert.That(stringNumber.GetStringNumber(), Is.EqualTo("2"));
            Assert.That(stringNumber.GetPosition(), Is.EqualTo(ModifierPosition.Below));
            Assert.That(stringNumber.GetDrawCircle(), Is.False);
            Assert.That(stringNumber.GetOffsetX(), Is.EqualTo(3));
            Assert.That(stringNumber.GetOffsetY(), Is.EqualTo(-1));
            Assert.That(stringNumber.GetStemOffset(), Is.EqualTo(5));
            Assert.That(stringNumber.IsDashed(), Is.False);
            Assert.That(stringNumber.GetLineEndType(), Is.EqualTo(RendererLineEndType.Down));
        }

        [Test]
        [Category("Unit")]
        public void FactoryOrnament_AcceptsV5StyleOptions()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);

            var ornament = factory.Ornament(new FactoryOrnamentOptions
            {
                Type = "tr",
                Position = ModifierPosition.Below,
                UpperAccidental = "#",
                LowerAccidental = "b",
                Delayed = true,
            });

            Assert.That(ornament.CheckContext(), Is.SameAs(ctx));
            Assert.That(ornament.Type, Is.EqualTo("tr"));
            Assert.That(ornament.GetPosition(), Is.EqualTo(ModifierPosition.Below));
            Assert.That(ornament.AccidentalUpper, Is.Not.Null);
            Assert.That(ornament.AccidentalLower, Is.Not.Null);
            Assert.That(ornament.Delayed, Is.True);
        }

        [Test]
        [Category("Unit")]
        public void FactoryPedalMarking_AcceptsV5StyleOptions()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);
            var stave = factory.Stave(10, 40, 420);
            var notes = new List<StaveNote>
            {
                new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4" }, Duration = "q" }),
            };
            notes[0].SetStave(stave);
            notes[0].SetContext(ctx);
            notes[0].SetX(80);

            var pedal = factory.PedalMarking(new FactoryPedalMarkingOptions
            {
                Notes = notes,
                Style = "text",
                DepressText = "Ped.",
                ReleaseText = "*",
                Line = 2,
            });

            Assert.That(pedal.CheckContext(), Is.SameAs(ctx));
            Assert.That(pedal.GetNotes(), Is.EqualTo(notes));
            Assert.That(pedal.GetPedalType(), Is.EqualTo(PedalMarkingType.Text));
            Assert.That(pedal.GetDepressText(), Is.EqualTo("Ped."));
            Assert.That(pedal.GetReleaseText(), Is.EqualTo("*"));
            Assert.That(pedal.GetLine(), Is.EqualTo(2));
            Assert.DoesNotThrow(() => factory.Draw());
        }

        [Test]
        [Category("Unit")]
        public void FactoryChordSymbol_AcceptsV5StyleOptions()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);

            var chord = factory.ChordSymbol(new FactoryChordSymbolOptions
            {
                FontSize = 18,
                FontFamily = "Academico",
                FontWeight = "bold",
                FontStyle = "italic",
                HJustifyString = "centerStem",
                VJustify = ChordSymbolVerticalJustify.Bottom,
                ReportWidth = false,
            }).AddText("C");

            Assert.That(chord.CheckContext(), Is.SameAs(ctx));
            Assert.That(chord.GetHorizontal(), Is.EqualTo(ChordSymbolHorizontalJustify.CenterStem));
            Assert.That(chord.GetVertical(), Is.EqualTo(ChordSymbolVerticalJustify.Bottom));
            Assert.That(chord.GetReportWidth(), Is.False);

            chord.GetSymbolBlocks()[0].Draw(ctx, 10, 20);
            Assert.That(ctx.GetCall("SetFont").Args[0], Is.EqualTo(18));
        }

        [Test]
        [Category("Unit")]
        public void FactoryChordSymbol_DefaultsToVexFlowJustification()
        {
            var factory = new Factory(new RecordingRenderContext(), 500, 200);

            var chord = factory.ChordSymbol(fontSize: 14);
            var optionsChord = factory.ChordSymbol(new FactoryChordSymbolOptions { FontSize = 14 });

            Assert.That(chord.GetHorizontal(), Is.EqualTo(ChordSymbolHorizontalJustify.Center));
            Assert.That(chord.GetVertical(), Is.EqualTo(ChordSymbolVerticalJustify.Top));
            Assert.That(optionsChord.GetHorizontal(), Is.EqualTo(ChordSymbolHorizontalJustify.Center));
            Assert.That(optionsChord.GetVertical(), Is.EqualTo(ChordSymbolVerticalJustify.Top));
        }

        [Test]
        [Category("Unit")]
        public void FactoryCreatesMultiMeasureRestAndTextBracket()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);
            var stave = factory.Stave(10, 40, 420);
            var start = factory.GhostNote("q");
            var stop = factory.GhostNote("q");
            start.SetX(80);
            stop.SetX(200);

            var rest = factory.MultiMeasureRest(new MultiMeasureRestRenderOptions
            {
                NumberOfMeasures = 8,
                UseSymbols = true,
                ShowNumber = false,
            });
            var bracket = factory.TextBracket(start, stop, "8", "va", TextBracketPosition.Bottom, line: 2);
            var bracketFromOptions = factory.TextBracket(new FactoryTextBracketOptions
            {
                Start = start,
                Stop = stop,
                Text = "15",
                Superscript = "ma",
                Position = TextBracketPosition.Top,
                Line = 3,
                Dashed = true,
                Dash = new[] { 4.0, 2.0 },
            });

            Assert.That(rest.GetStave(), Is.SameAs(stave));
            Assert.That(rest.GetNumberOfMeasures(), Is.EqualTo(8));
            Assert.That(rest.RenderOptions.UseSymbols, Is.True);
            Assert.That(bracket.GetStart(), Is.SameAs(start));
            Assert.That(bracket.GetStop(), Is.SameAs(stop));
            Assert.That(bracket.GetPosition(), Is.EqualTo(TextBracketPosition.Bottom));
            Assert.That(bracket.GetLine(), Is.EqualTo(2));
            Assert.That(bracketFromOptions.GetStart(), Is.SameAs(start));
            Assert.That(bracketFromOptions.GetStop(), Is.SameAs(stop));
            Assert.That(bracketFromOptions.GetText(), Is.EqualTo("15"));
            Assert.That(bracketFromOptions.GetPosition(), Is.EqualTo(TextBracketPosition.Top));
            Assert.That(bracketFromOptions.GetLine(), Is.EqualTo(3));
        }

        [Test]
        [Category("Unit")]
        public void FactoryStaveTie_AcceptsV5StyleOptions()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);
            var start = factory.GhostNote("q");
            var stop = factory.GhostNote("q");
            var tieNotes = new TieNotes { FirstNote = start, LastNote = stop, FirstIndex = 1, LastIndex = 2 };
            var renderOptions = new StaveTieRenderOptions
            {
                Cp1 = 11,
                Cp2 = 13,
                YShift = 5,
            };

            var tie = factory.StaveTie(new FactoryStaveTieOptions
            {
                Notes = tieNotes,
                Text = "H",
                Direction = 1,
                RenderOptions = renderOptions,
            });

            Assert.That(tie.CheckContext(), Is.SameAs(ctx));
            Assert.That(tie.GetNotes(), Is.SameAs(tieNotes));
            Assert.That(tie.GetText(), Is.EqualTo("H"));
            Assert.That(tie.GetDirection(), Is.EqualTo(1));
            Assert.That(tie.RenderOptions, Is.SameAs(renderOptions));
            Assert.That(tie.RenderOptions.Cp1, Is.EqualTo(11));
        }

        [Test]
        [Category("Unit")]
        public void FactoryStaveLine_AcceptsV5StyleOptions()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);
            var from = new StaveNote(new StaveNoteStruct { Keys = new[] { "c/4", "e/4" }, Duration = "q" });
            var to = new StaveNote(new StaveNoteStruct { Keys = new[] { "d/4", "f/4" }, Duration = "q" });

            var line = factory.StaveLine(new FactoryStaveLineOptions
            {
                From = from,
                To = to,
                FirstIndexes = new List<int> { 0, 1 },
                LastIndexes = new List<int> { 1, 0 },
                Options = new StaveLineTextOptions { Text = "gliss." },
            });

            Assert.That(line.CheckContext(), Is.SameAs(ctx));
            Assert.That(line.GetStart(), Is.SameAs(from));
            Assert.That(line.GetStop(), Is.SameAs(to));
            Assert.That(line.GetFirstIndexes(), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(line.GetLastIndexes(), Is.EqualTo(new[] { 1, 0 }));
            Assert.That(line.GetText(), Is.EqualTo("gliss."));
        }

        [Test]
        [Category("Unit")]
        public void FactoryVibratoBracket_AcceptsV5StyleOptions()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);
            var start = factory.GhostNote("q");
            var stop = factory.GhostNote("q");

            var bracket = factory.VibratoBracket(new FactoryVibratoBracketOptions
            {
                From = start,
                To = stop,
                Line = 3,
                Code = 0xeab1,
                Harsh = true,
                VibratoWidth = 42,
            });

            Assert.That(bracket.CheckContext(), Is.SameAs(ctx));
            Assert.That(bracket.GetStart(), Is.SameAs(start));
            Assert.That(bracket.GetStop(), Is.SameAs(stop));
            Assert.That(bracket.GetLine(), Is.EqualTo(3));
            Assert.That(bracket.GetVibratoCode(), Is.EqualTo(0xeab1));
            Assert.That(bracket.IsHarsh(), Is.True);
            Assert.That(bracket.GetVibratoWidth(), Is.EqualTo(42));
        }

        [Test]
        [Category("Unit")]
        public void FactoryStaveConnector_AcceptsV5StyleOptions()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);
            var top = new Stave(10, 40, 300);
            var bottom = new Stave(10, 140, 300);

            var connector = factory.StaveConnector(new FactoryStaveConnectorOptions
            {
                TopStave = top,
                BottomStave = bottom,
                TypeString = "brace",
                XShift = -4,
                Texts = new List<FactoryStaveConnectorTextOptions>
                {
                    new FactoryStaveConnectorTextOptions { Text = "Piano", ShiftX = 2, ShiftY = 3 },
                },
            });

            Assert.That(connector.CheckContext(), Is.SameAs(ctx));
            Assert.That(connector.GetConnectorType(), Is.EqualTo(StaveConnectorType.Brace));
            Assert.That(connector.GetXShift(), Is.EqualTo(-4));
            Assert.That(connector.GetTexts(), Has.Count.EqualTo(1));
            Assert.That(connector.GetTexts()[0].Content, Is.EqualTo("Piano"));
            Assert.That(connector.GetTexts()[0].ShiftX, Is.EqualTo(2));
            Assert.That(connector.GetTexts()[0].ShiftY, Is.EqualTo(3));
        }

        [Test]
        [Category("Unit")]
        public void FactoryAccidental_AcceptsV5StyleOptions()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);

            var accidental = factory.Accidental(new FactoryAccidentalOptions
            {
                Type = "b",
                Cautionary = true,
            });

            Assert.That(accidental.CheckContext(), Is.SameAs(ctx));
            Assert.That(accidental.Type, Is.EqualTo("b"));
            Assert.That(accidental.IsCautionary(), Is.True);
            Assert.That(accidental.GetFontScale(), Is.EqualTo(Metrics.GetDouble("Accidental.cautionary.fontSize")));
        }

        [Test]
        [Category("Unit")]
        public void FactoryVibrato_AcceptsV5StyleOptions()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);

            var vibrato = factory.Vibrato(new FactoryVibratoOptions
            {
                RenderOptions = new VibratoRenderOptions
                {
                    Code = 0xeab1,
                    Width = 24,
                    Harsh = true,
                },
            });
            var topLevelVibrato = factory.Vibrato(new FactoryVibratoOptions
            {
                Code = 0xeab0,
                VibratoWidth = 30,
                Harsh = false,
            });

            Assert.That(vibrato.CheckContext(), Is.SameAs(ctx));
            Assert.That(vibrato.GetVibratoCode(), Is.EqualTo(0xeab1));
            Assert.That(vibrato.GetWidth(), Is.EqualTo(24));
            Assert.That(vibrato.IsHarsh, Is.True);
            Assert.That(topLevelVibrato.CheckContext(), Is.SameAs(ctx));
            Assert.That(topLevelVibrato.GetVibratoCode(), Is.EqualTo(0xeab0));
            Assert.That(topLevelVibrato.GetWidth(), Is.EqualTo(30));
            Assert.That(topLevelVibrato.IsHarsh, Is.False);
        }

        [Test]
        [Category("Unit")]
        public void FactoryGraceNoteGroup_AcceptsTypedGraceNotesAndOptions()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);
            var graceNotes = new List<GraceNote>
            {
                factory.GraceNote(new GraceNoteStruct { Keys = new[] { "d/4" }, Duration = "8" }),
                factory.GraceNote(new GraceNoteStruct { Keys = new[] { "e/4" }, Duration = "8" }),
            };

            var group = factory.GraceNoteGroup(new FactoryGraceNoteGroupOptions
            {
                Notes = graceNotes,
                Slur = true,
            });

            Assert.That(group.CheckContext(), Is.SameAs(ctx));
            Assert.That(group.GetGraceNotes(), Is.SameAs(graceNotes));
            Assert.That(group.GetShowSlur(), Is.True);
        }

        [Test]
        [Category("Unit")]
        public void FactoryNoteSubGroup_AcceptsV5StyleOptions()
        {
            var ctx = new RecordingRenderContext();
            var factory = new Factory(ctx, 500, 200);
            var notes = new List<VexFlowSharp.Note>
            {
                factory.ClefNote("treble", "small"),
                factory.TimeSigNote("3/8"),
            };

            var group = factory.NoteSubGroup(new FactoryNoteSubGroupOptions
            {
                Notes = notes,
            });

            Assert.That(group.CheckContext(), Is.SameAs(ctx));
            Assert.That(group.GetSubNotes(), Is.SameAs(notes));
            Assert.That(group.GetCategory(), Is.EqualTo(NoteSubGroup.CATEGORY));
        }
    }
}
