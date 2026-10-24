using UnityEngine;

namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
        private const float BackgroundMusicGain=.336f;
        private AudioSource backgroundMusic;
        private ResourceRequest backgroundMusicRequest;

        private void InitializeBackgroundMusic()
        {
            backgroundMusic=gameObject.AddComponent<AudioSource>();
            backgroundMusic.playOnAwake=false;backgroundMusic.loop=true;backgroundMusic.spatialBlend=0;
            // SOUND continues to control SFX through the listener; MUSIC is independent.
            backgroundMusic.ignoreListenerVolume=true;
            backgroundMusic.volume=Mathf.Clamp01(PlayerPrefs.GetFloat("WM_MusicVolume",1f))*BackgroundMusicGain;
            backgroundMusicRequest=Resources.LoadAsync<AudioClip>("WorkshopMatch/Audio/wizard_music");
        }

        private void UpdateBackgroundMusic()
        {
            // Menu/level transitions stop coroutines; loading must survive those transitions.
            if(backgroundMusicRequest!=null&&backgroundMusicRequest.isDone)
            {
                backgroundMusic.clip=backgroundMusicRequest.asset as AudioClip;
                backgroundMusicRequest=null;
            }
            // Opening SFX has the stage to itself; start music only after the gate closes.
            if(!openingTiming.Visible&&backgroundMusic!=null&&backgroundMusic.clip!=null&&!backgroundMusic.isPlaying)
                backgroundMusic.Play();
        }

        private void SetBackgroundMusicVolume(float value)
        {
            value=Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("WM_MusicVolume",value);PlayerPrefs.Save();
            if(backgroundMusic!=null)backgroundMusic.volume=value*BackgroundMusicGain;
        }

        private void StopBackgroundMusic()
        {
            backgroundMusicRequest=null;
            if(backgroundMusic==null)return;
            backgroundMusic.Stop();backgroundMusic.clip=null;
        }
    }
}
