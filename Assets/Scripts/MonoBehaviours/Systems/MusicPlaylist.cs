using UnityEngine;

public class MusicPlaylist : MonoBehaviour
{
    [Header("Playlist")]
    [SerializeField] private AudioClip[] tracks;
    [SerializeField] private float volume = 0.09f;
    [SerializeField] private float fadeDuration = 2f;

    private AudioSource _audioSource;
    private int _currentIndex = 0;
    private float _fadeTimer;
    private bool _isFading;

    private static MusicPlaylist _instance;

    private void Awake()
    {
        // Singleton: destroy duplicates when DontDestroyOnLoad carries us across scenes
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.loop = false;
        _audioSource.spatialBlend = 0f;
        _audioSource.playOnAwake = false;
        _audioSource.volume = volume;
        _audioSource.priority = 180;
        _audioSource.dopplerLevel = 0f;
        _audioSource.bypassReverbZones = true;
    }

    private void Start()
    {
        if (tracks != null && tracks.Length > 0)
        {
            PlayTrack(_currentIndex);
        }
    }

    private void Update()
    {
        if (tracks == null || tracks.Length == 0) return;

        if (_isFading)
        {
            _fadeTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(_fadeTimer / fadeDuration);
            _audioSource.volume = t * volume;

            if (_fadeTimer <= 0f)
            {
                _isFading = false;
                _currentIndex = (_currentIndex + 1) % tracks.Length;
                PlayTrack(_currentIndex);
            }
            return;
        }

        // Start fade when track has ~fadeDuration seconds remaining
        if (_audioSource.isPlaying && _audioSource.clip != null)
        {
            float remaining = _audioSource.clip.length - _audioSource.time;
            if (remaining <= fadeDuration)
            {
                _isFading = true;
                _fadeTimer = fadeDuration;
            }
        }
    }

    private void PlayTrack(int index)
    {
        if (tracks[index] == null) return;

        _audioSource.clip = tracks[index];
        _audioSource.volume = volume;
        _audioSource.Play();
    }


}
