using UnityEngine;
using System.Runtime.InteropServices;
namespace WorkshopMatch
{
    public sealed partial class BoardBlockoutController
    {
        private AudioSource chainAudio;
        private AudioClip chainTone;
        private float chainShakeAt=-10f,chainShakeAmount,lastChainHapticAt=-10f;
        private const float RocketCellSeconds=.035f;
        private static float RayCellDelay(Match3BoardModel.PowerEvent power,Match3BoardModel.Cell cell)
        {
            // Parallel wide lanes share the launch column/row; their sideways offset adds no delay.
            int distance=power.Shape==Match3BoardModel.PowerShape.WideRay
                ? power.Kind==SpecialKind.RayVertical?Mathf.Abs(cell.Y-power.Cell.Y):Mathf.Abs(cell.X-power.Cell.X)
                : Mathf.Max(Mathf.Abs(cell.X-power.Cell.X),Mathf.Abs(cell.Y-power.Cell.Y));
            return distance*RocketCellSeconds;
        }
        private void PlayChainFeedback()
        {
            int level=Mathf.Clamp(automaticChain,1,10);
            if(chainAudio==null){chainAudio=gameObject.AddComponent<AudioSource>();chainAudio.playOnAwake=false;chainAudio.spatialBlend=0;}
            if(chainTone==null)
            {
                const int rate=22050;
                float[] samples=new float[(int)(rate*.14f)];
                for(int i=0;i<samples.Length;i++)
                {
                    float t=i/(float)rate;
                    samples[i]=(Mathf.Sin(t*330*Mathf.PI*2)+.18f*Mathf.Sin(t*660*Mathf.PI*2))*Mathf.Min(1,t*700)*Mathf.Exp(-t*32)*.18f;
                }
                chainTone=AudioClip.Create("Cascade soft chime",samples.Length,1,rate,false);chainTone.SetData(samples,0);
            }
            chainAudio.pitch=Mathf.Lerp(.72f,1.65f,(level-1)/9f);chainAudio.PlayOneShot(chainTone);
            if(automaticChain>=4){chainShakeAt=Time.unscaledTime;chainShakeAmount=Mathf.Lerp(.0025f,.008f,(level-4)/6f);}
            if(Time.unscaledTime-lastChainHapticAt>=.065f){lastChainHapticAt=Time.unscaledTime;TriggerChainHaptic();}
        }
        private Vector2 ChainShakeOffset(Rect portrait)
        {
            float age=Time.unscaledTime-chainShakeAt;
            if(screen!=AppScreen.Playing||settingsOpen||age>=.28f)return Vector2.zero;
            return new Vector2(Mathf.Sin(age*91),Mathf.Sin(age*113+.6f))*portrait.width*chainShakeAmount*(1-age/.28f);
        }
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void WMChainHaptic();
#endif
        private static void TriggerChainHaptic()
        {
#if UNITY_IOS && !UNITY_EDITOR
            WMChainHaptic();
#elif UNITY_ANDROID && !UNITY_EDITOR
            // View feedback respects the device haptic setting and needs no permission.
            try
            {
                using var unity=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                using var activity=unity.GetStatic<AndroidJavaObject>("currentActivity");
                activity.Call("runOnUiThread",new AndroidJavaRunnable(()=>
                {
                    using var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var current=player.GetStatic<AndroidJavaObject>("currentActivity");
                    using var window=current.Call<AndroidJavaObject>("getWindow");
                    using var decor=window.Call<AndroidJavaObject>("getDecorView");
                    decor.Call<bool>("performHapticFeedback",4); // CLOCK_TICK
                }));
            }
            catch(AndroidJavaException) { }
#endif
        }
    }
}
