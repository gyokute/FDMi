
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.Udon;
namespace SaccFlight_FDMi
{
    public class FDMi_Annunciator : FDMi_Avionics
    {
        [SerializeField] private Graphic graphic;
        [SerializeField] private Color[] colors;
        public virtual void whenChange(int i)
        {
            graphic.color = colors[i];
        }

    }
}