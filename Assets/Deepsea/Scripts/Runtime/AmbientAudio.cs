using UnityEngine;

namespace Deepsea
{
    public sealed class AmbientAudio : MonoBehaviour
    {
        public GameDirector game;
        public float MusicVolume=.45f;
        public float EffectsVolume=.55f;
        AudioSource ambience,engine,effects;
        AudioClip discovery,home,scan;
        void Start()
        {
            MusicVolume=PlayerPrefs.GetFloat("deepsea-music",.45f);
            EffectsVolume=PlayerPrefs.GetFloat("deepsea-effects",.55f);
            ambience=gameObject.AddComponent<AudioSource>();ambience.loop=true;ambience.clip=Music();ambience.Play();
            engine=gameObject.AddComponent<AudioSource>();engine.loop=true;engine.clip=Engine();engine.Play();
            effects=gameObject.AddComponent<AudioSource>();
            discovery=Chime("发现",440,.7f);home=Chime("归航",330,1.2f);scan=Chime("探测",180,.35f);
        }
        void Update()
        {
            if(ambience==null)return;
            ambience.volume=MusicVolume*.5f;
            engine.volume=EffectsVolume*Mathf.Clamp01(game.Boat.Speed/5)*.18f;
            engine.pitch=.8f+game.Boat.Speed*.075f;
            effects.volume=EffectsVolume*.5f;
        }
        public void PlayCue(bool harbour){if(effects!=null)effects.PlayOneShot(harbour?home:discovery);}
        public void PlayScan(){if(effects!=null)effects.PlayOneShot(scan);}
        static AudioClip Engine()
        {
            int rate=22050;float[] data=new float[rate];
            for(int i=0;i<data.Length;i++)
            {float t=i/(float)rate;data[i]=(Mathf.Sin(t*2*Mathf.PI*55)*.35f+Mathf.Sin(t*2*Mathf.PI*110)*.12f)*(1+Mathf.Sin(t*2*Mathf.PI*7)*.14f);}
            AudioClip clip=AudioClip.Create("原型推进器",data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        static AudioClip Music()
        {
            int rate=22050;float seconds=16;float[] data=new float[(int)(rate*seconds)];
            float[] notes={110,130.8128f,146.8324f,164.8138f,195.9977f,164.8138f,146.8324f,130.8128f};
            var random=new System.Random(881);float filtered=0;
            for(int i=0;i<data.Length;i++)
            {
                float t=i/(float)rate;
                float envelope=Mathf.Sin(t/seconds*Mathf.PI);envelope*=envelope;
                float bed=(Mathf.Sin(t*2*Mathf.PI*110)+Mathf.Sin(t*2*Mathf.PI*164.8138f)*.5f+Mathf.Sin(t*2*Mathf.PI*220)*.25f)*.10f;
                float local=t%2f;float note=notes[(int)(t/2f)%notes.Length];
                float melody=Mathf.Sin(t*2*Mathf.PI*note*2)*Mathf.Sin(local/2*Mathf.PI)*.09f;
                filtered=filtered*.995f+((float)random.NextDouble()*2-1)*.005f;
                data[i]=(bed+melody+filtered*.65f)*envelope;
            }
            AudioClip clip=AudioClip.Create("原型海湾氛围循环",data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        static AudioClip Chime(string name,float frequency,float seconds)
        {
            int rate=22050;float[] data=new float[(int)(rate*seconds)];
            for(int i=0;i<data.Length;i++)
            {float t=i/(float)rate;data[i]=(Mathf.Sin(t*frequency*2*Mathf.PI)*.25f+Mathf.Sin(t*frequency*3*Mathf.PI)*.11f)*Mathf.Exp(-t*6)*Mathf.Min(1,t*80);}
            AudioClip clip=AudioClip.Create(name,data.Length,1,rate,false);clip.SetData(data,0);return clip;
        }
        void OnDestroy(){PlayerPrefs.SetFloat("deepsea-music",MusicVolume);PlayerPrefs.SetFloat("deepsea-effects",EffectsVolume);}
    }
}
