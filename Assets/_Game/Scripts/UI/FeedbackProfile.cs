using UnityEngine;

namespace BossNoTakeAnyMore.UI
{
    [CreateAssetMenu(menuName = "Warehouse/Feedback Profile")]
    public sealed class FeedbackProfile : ScriptableObject
    {
        [Header("Audio / replace clips here")]
        [SerializeField] private AudioClip _pickup;
        [SerializeField] private AudioClip _rotate;
        [SerializeField] private AudioClip _place;
        [SerializeField] private AudioClip _invalid;
        [SerializeField] private AudioClip _return;
        [SerializeField] private AudioClip _complete;
        [SerializeField, Range(0, 1)] private float _volume = 0.65f;
        [Header("Landing / design-space units")]
        [SerializeField, Range(0.08f, 0.4f)] private float _landingDuration = 0.18f;
        [SerializeField, Range(0, 0.15f)] private float _bounce = 0.065f;
        [Header("Motion / render-only, does not move hit targets")]
        [SerializeField, Range(0, 10)] private float _placeShake = 2.5f;
        [SerializeField, Range(0, 12)] private float _completeShake = 5;
        [SerializeField, Range(0.05f, 0.4f)] private float _shakeDuration = 0.16f;
        [SerializeField, Range(0.35f, 1.5f)] private float _completionDelay = 0.85f;
        public AudioClip Pickup => _pickup;
        public AudioClip Rotate => _rotate;
        public AudioClip Place => _place;
        public AudioClip Invalid => _invalid;
        public AudioClip Return => _return;
        public AudioClip Complete => _complete;
        public float Volume => _volume;
        public float LandingDuration => _landingDuration;
        public float Bounce => _bounce;
        public float PlaceShake => _placeShake;
        public float CompleteShake => _completeShake;
        public float ShakeDuration => _shakeDuration;
        public float CompletionDelay => _completionDelay;
    }
}
