using UnityEngine;

namespace Platformer3D
{
    /// <summary>
    /// Senior-level Player Visual Effects (VFX) System.
    /// Procedurally creates and triggers particle effects for Jump, Land, and Slide actions.
    /// </summary>
    public class PlayerVFX : MonoBehaviour
    {
        [Header("Particle Customization")]
        [SerializeField] private Color dustColor = new Color(0.85f, 0.85f, 0.8f, 0.5f);
        [SerializeField] private Color slideSparkColor = new Color(1.0f, 0.7f, 0.2f, 0.8f);

        private ParticleSystem jumpParticles;
        private ParticleSystem landParticles;
        private ParticleSystem slideParticles;

        private void Awake()
        {
            CreateJumpParticleSystem();
            CreateLandParticleSystem();
            CreateSlideParticleSystem();
        }

        private void CreateJumpParticleSystem()
        {
            GameObject jumpObj = new GameObject("VFX_JumpDust");
            jumpObj.transform.SetParent(transform);
            jumpObj.transform.localPosition = Vector3.zero;

            jumpParticles = jumpObj.AddComponent<ParticleSystem>();
            var main = jumpParticles.main;
            main.playOnAwake = false;
            main.duration = 0.2f;
            main.startLifetime = 0.35f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startColor = dustColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = jumpParticles.emission;
            emission.enabled = false;

            var shape = jumpParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
            shape.radius = 0.3f;
        }

        private void CreateLandParticleSystem()
        {
            GameObject landObj = new GameObject("VFX_LandDust");
            landObj.transform.SetParent(transform);
            landObj.transform.localPosition = Vector3.zero;

            landParticles = landObj.AddComponent<ParticleSystem>();
            var main = landParticles.main;
            main.playOnAwake = false;
            main.duration = 0.2f;
            main.startLifetime = 0.4f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 5.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startColor = dustColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = landParticles.emission;
            emission.enabled = false;

            var shape = landParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Donut;
            shape.radius = 0.4f;
            shape.donutRadius = 0.1f;
        }

        private void CreateSlideParticleSystem()
        {
            GameObject slideObj = new GameObject("VFX_SlideTrail");
            slideObj.transform.SetParent(transform);
            slideObj.transform.localPosition = Vector3.zero;

            slideParticles = slideObj.AddComponent<ParticleSystem>();
            var main = slideParticles.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = 0.3f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startColor = slideSparkColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = slideParticles.emission;
            emission.rateOverTime = 30f;

            var shape = slideParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;
        }

        public void PlayJumpVFX()
        {
            if (jumpParticles != null)
            {
                jumpParticles.transform.position = transform.position;
                jumpParticles.Emit(18);
            }
        }

        public void PlayLandVFX()
        {
            if (landParticles != null)
            {
                landParticles.transform.position = transform.position;
                landParticles.Emit(25);
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
