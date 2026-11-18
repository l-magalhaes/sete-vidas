using UnityEngine;

namespace SeteVidas.World
{
    /// <summary>
    /// A câmera nas mãos de uma cena (a flecha da Vulpis na luta com a Isyathy): desliga o Cinemachine, move a câmera e
    /// dá zoom em degraus inteiros, para continuar pixel-perfect (a <c>PixelPerfectCamera</c> do URP recebe uma resolução
    /// de referência menor). Funciona com o jogo pausado (quem chama anda no tempo real). <see cref="End"/> devolve
    /// tudo como estava. Os componentes são achados pelo nome do tipo, para o runtime não depender do URP nem do
    /// Cinemachine.
    /// </summary>
    public static class CinematicCamera
    {
        static Camera s_cam;
        static Behaviour s_brain;
        static Component s_ppc;
        static int s_refX, s_refY;
        static float s_size;
        static Vector3 s_home;
        static float s_zoom = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_cam = null; s_brain = null; s_ppc = null; s_refX = s_refY = 0; s_size = 0f; s_home = Vector3.zero; s_zoom = 1f; } // Play sem recarregar o domínio

        public static bool Active => s_cam != null;
        /// <summary>Onde a câmera estava ao começar (o gato): a volta termina aqui.</summary>
        public static Vector2 Home => s_home;
        public static Vector2 Position => s_cam != null ? (Vector2)s_cam.transform.position : (Vector2)s_home;
        public static float Zoom => s_zoom;

        /// <summary>Degraus do zoom (resolução de referência ÷ zoom). Zoom 1 = a tela normal.</summary>
        public static Vector2Int Reference(int refX, int refY, float zoom)
        {
            zoom = Mathf.Max(1f, zoom);
            int y = Mathf.Max(1, Mathf.RoundToInt(refY / zoom));
            int x = Mathf.Max(1, Mathf.RoundToInt(y * (float)refX / refY));
            return new Vector2Int(x, y);
        }

        public static bool Begin()
        {
            if (Active) return true;
            var cam = Camera.main;
            if (cam == null) return false;
            s_cam = cam;
            s_home = cam.transform.position;
            s_size = cam.orthographicSize;
            s_zoom = 1f;
            s_brain = null; s_ppc = null;
            foreach (var c in cam.GetComponents<Component>())
            {
                if (c == null) continue;
                string n = c.GetType().Name;
                if (n == "CinemachineBrain") s_brain = c as Behaviour;
                else if (n == "PixelPerfectCamera") s_ppc = c;
            }
            if (s_brain != null) s_brain.enabled = false;
            if (s_ppc != null) { s_refX = GetInt(s_ppc, "refResolutionX"); s_refY = GetInt(s_ppc, "refResolutionY"); }
            return true;
        }

        public static void MoveTo(Vector2 p)
        {
            if (s_cam == null) return;
            s_cam.transform.position = new Vector3(p.x, p.y, s_home.z);
        }

        /// <summary>Zoom (1 = normal, 2 = o dobro). Com a câmera pixel-perfect, só muda quando o degrau inteiro muda.</summary>
        public static void SetZoom(float zoom)
        {
            if (s_cam == null) return;
            s_zoom = Mathf.Max(1f, zoom);
            if (s_ppc != null && s_refX > 0 && s_refY > 0)
            {
                var r = Reference(s_refX, s_refY, s_zoom);
                SetInt(s_ppc, "refResolutionX", r.x);
                SetInt(s_ppc, "refResolutionY", r.y);
            }
            else s_cam.orthographicSize = s_size / s_zoom;
        }

        public static void End()
        {
            if (s_cam == null) return;
            if (s_ppc != null && s_refX > 0) { SetInt(s_ppc, "refResolutionX", s_refX); SetInt(s_ppc, "refResolutionY", s_refY); }
            else s_cam.orthographicSize = s_size;
            s_cam.transform.position = s_home;
            if (s_brain != null) s_brain.enabled = true;
            s_cam = null; s_brain = null; s_ppc = null; s_zoom = 1f;
        }

        static int GetInt(Component c, string prop)
        {
            var p = c.GetType().GetProperty(prop);
            return p != null ? (int)p.GetValue(c) : 0;
        }

        static void SetInt(Component c, string prop, int v)
        {
            var p = c.GetType().GetProperty(prop);
            if (p != null && p.CanWrite) p.SetValue(c, v);
        }
    }
}
