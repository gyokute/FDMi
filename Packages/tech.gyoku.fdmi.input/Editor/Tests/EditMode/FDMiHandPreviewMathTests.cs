using NUnit.Framework;

namespace FDMi.input.Editor.Tests
{
    public class FDMiHandPreviewMathTests
    {
        [Test]
        public void LeverHeld_AddsScaledTravel()
        {
            float value = FDMiHandPreviewMath.LeverHeld(0.2f, 0.5f, 2f, 0f, 1f, 0f, false);
            Assert.That(value, Is.EqualTo(1f).Within(1e-5f));
        }

        [Test]
        public void LeverHeld_ClampsToBounds()
        {
            Assert.That(
                FDMiHandPreviewMath.LeverHeld(0f, 2f, 1f, 0f, 1f, 0f, false),
                Is.EqualTo(1f)
            );
            Assert.That(
                FDMiHandPreviewMath.LeverHeld(1f, -2f, 1f, 0f, 1f, 0f, false),
                Is.EqualTo(0f)
            );
        }

        [Test]
        public void LeverHeld_RepeatsWhenEnabled()
        {
            float value = FDMiHandPreviewMath.LeverHeld(0f, 1.5f, 1f, 0f, 1f, 0f, true);
            Assert.That(value, Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void LeverHeld_RoundsToStep()
        {
            float value = FDMiHandPreviewMath.LeverHeld(0f, 0.26f, 1f, 0f, 1f, 0.1f, false);
            Assert.That(value, Is.EqualTo(0.3f).Within(1e-5f));
        }

        [Test]
        public void LeverRelease_UsesNearestDetent()
        {
            float value = FDMiHandPreviewMath.LeverRelease(
                0f,
                0.4f,
                1f,
                0f,
                1f,
                0f,
                false,
                new[] { 0f, 0.5f, 1f }
            );
            Assert.That(value, Is.EqualTo(0.5f));
        }

        [Test]
        public void LeverRelease_WithoutDetents_RoundsToStep()
        {
            float value = FDMiHandPreviewMath.LeverRelease(
                0f,
                0.26f,
                1f,
                0f,
                1f,
                0.1f,
                false,
                new float[0]
            );
            Assert.That(value, Is.EqualTo(0.3f).Within(1e-5f));
        }

        [Test]
        public void YokeOutput_ClampsToMinusOnePlusOne()
        {
            Assert.That(FDMiHandPreviewMath.YokeOutput(0.5f, 1f), Is.EqualTo(0.5f));
            Assert.That(FDMiHandPreviewMath.YokeOutput(2f, 1f), Is.EqualTo(1f));
            Assert.That(FDMiHandPreviewMath.YokeOutput(-2f, 1f), Is.EqualTo(-1f));
        }

        [Test]
        public void TravelToBound_DividesByMultiplier()
        {
            Assert.That(
                FDMiHandPreviewMath.TravelToBound(0f, 1f, 0.5f),
                Is.EqualTo(2f).Within(1e-5f)
            );
            Assert.That(FDMiHandPreviewMath.TravelToBound(0f, 1f, 0f), Is.EqualTo(0f));
        }
    }
}
