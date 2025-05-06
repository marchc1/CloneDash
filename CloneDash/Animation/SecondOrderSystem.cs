namespace CloneDash.Animation
{
    /// <summary>
    /// Second order system, for animation smoothing mostly
    /// </summary>
    public class SecondOrderSystem
    {
        private static readonly float PI = (float)Math.PI;
        private float xp;
        private float y, yd;
        private float k1, k2, k3;
        private float T_crit;
        private DateTime last = DateTime.UtcNow;
		/// <summary>
		/// Entirely from https://www.youtube.com/watch?v=KPoeNZZ6H4s
		/// </summary>
		/// <param name="f">Natural frequency, the speed that the system will respond to changes, as well as frequency of vibrations</param>
		/// <param name="z">Damping coefficient, describes how the system comes to settle at the target. When Z is 0, vibration will never die down. When greater then 1, the system will not vibrate and will slowly reach the target.</param>
		/// <param name="r">Initial response, when 0, the system takes time to begin accelerating. When positive, it reacts immediately. When greater then 1, it will overshoot. When negative, it will anticipate.</param>
		/// <param name="x0"></param>

		private float f;
		private float z;
		private float r;
		public void ResetTo(float x0) {
			k1 = z / (PI * f);
			k2 = 1 / (2 * PI * f * (2 * PI * f));
			k3 = r * z / (2 * PI * f);

			T_crit = 0.8f * ((float)Math.Sqrt(4 * k2 + k1 * k1) - k1);

			xp = x0;
			y = x0;

			yd = 0;
			last = DateTime.UtcNow;
		}
		public SecondOrderSystem(float f, float z, float r, float x0)
        {
			this.f = f;
			this.z = z;
			this.r = r;
			ResetTo(x0);
        }
        public float Update(float x)
        {
            float deltatime = (float)(DateTime.UtcNow - last).TotalSeconds;
            return Update(deltatime, x);
        }
        public float Update(float T, float x, float? xdIn = null)
        {
            float xd = 0f;

            if (!xdIn.HasValue)
            {
                xd = (x - xp) / T;
                xp = x;
            }
            else
                xd = xdIn.Value;

            int iterations = (int)Math.Ceiling(T / T_crit);
            T = T / iterations;

            for (int i = 0; i < iterations; i++)
            {
                y = y + T * yd;
                yd = yd + T * (x + k3 * xd - y - k1 * yd) / k2;
            }

            last = DateTime.UtcNow;
            return y;
        }

		public float Out => y;
    }
}
