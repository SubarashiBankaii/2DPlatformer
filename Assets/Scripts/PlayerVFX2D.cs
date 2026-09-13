using UnityEngine;

namespace Platformer2D
{
    /// <summary>
    /// Simple 2D VFX: dust on jump/land, sparks on slide.
    /// Stops each ParticleSystem immediately after creation
    /// so we can safely configure duration and other properties.
    /// </summary>
    public class PlayerVFX2D : MonoBehaviour
    {
        [Header("Colors")]
        [SerializeField] private Color dustColor = new Color(0.9f, 0.9f, 0.85f, 0.6f);
        [SerializeField] private Color slideColor = new Color(1.0f, 0.65f, 0.2f, 0.8f);

        [SerializeField] private Color dashColor = new Color(0.0f, 0.9f, 1.0f, 0.8f);

        private ParticleSystem jumpParticles;
        private ParticleSystem landParticles;
        private ParticleSystem slideParticles;
        private ParticleSystem dashParticles;

        private void Awake()
        {
            jumpParticles = CreateSystem("VFX2D_JumpDust");
            landParticles = CreateSystem("VFX2D_LandDust");
            slideParticles = CreateSystem("VFX2D_SlideTrail");
            dashParticles = CreateSystem("VFX2D_DashBurst");

            ConfigureJump();
            ConfigureLand();
            ConfigureSlide();
            ConfigureDash();
        }

        /// <summary>
        /// Creates a ParticleSystem child and immediately stops it
        /// so we can modify duration without errors.
        /// </summary>
        private ParticleSystem CreateSystem(string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform);
            obj.transform.localPosition = Vector3.zero;

            var ps = obj.AddComponent<ParticleSystem>();
            // Stop immediately so we can set duration safely
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            return ps;
        }

        private void ConfigureJump()
        {
            var main = jumpParticles.main;
            main.playOnAwake = false;
            main.duration = 0.2f;
            main.startLifetime = 0.3f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startColor = dustColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = jumpParticles.emission;
            emission.enabled = false;

            var shape = jumpParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.2f;
        }

        private void ConfigureLand()
        {
            var main = landParticles.main;
            main.playOnAwake = false;
            main.duration = 0.2f;
            main.startLifetime = 0.35f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
            main.startColor = dustColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = landParticles.emission;
            emission.enabled = false;

            var shape = landParticles.shape;
            shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
            shape.radius = 0.4f;
        }

        private void ConfigureSlide()
        {
            var main = slideParticles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = 0.25f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
            main.startColor = slideColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = slideParticles.emission;
            emission.rateOverTime = 25f;
        }

        private void ConfigureDash()
        {
            var main = dashParticles.main;
            main.playOnAwake = false;
            main.duration = 0.25f;
            main.startLifetime = 0.3f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startColor = dashColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = dashParticles.emission;
            emission.enabled = false;

            var shape = dashParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f;
        }

        public void PlayJumpVFX()
        {
            if (jumpParticles != null)
            {
                jumpParticles.transform.position = transform.position;
                jumpParticles.Emit(14);
            }
        }

        public void PlayLandVFX()
        {
            if (landParticles != null)
            {
                landParticles.transform.position = transform.position;
                landParticles.Emit(20);
            }
        }

        public void PlayDashVFX()
        {
            if (dashParticles != null)
            {
                dashParticles.transform.position = transform.position;
                dashParticles.Emit(22);
            }
        }

        public void PlaySlideEffect(bool enable)
        {
            if (slideParticles == null) return;
            if (enable)
            {
                slideParticles.transform.position = transform.position;
                slideParticles.Play();
            }
            else
            {
                slideParticles.Stop();
            }
        }
    }
}
