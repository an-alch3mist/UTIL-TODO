using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using SPACE_UTIL;
using SPACE_DrawSystem;

namespace SPACE_DOODLE_FOURIER
{
	public class DoodleFourier : MonoBehaviour
	{
		private void Update()
		{
			if (INPUT.M.InstantDown(0))
			{
				StopAllCoroutines();
				StartCoroutine(STIMULATE());
			}
		}

		IEnumerator STIMULATE()
		{
			yield return null;

			// yield return checkComplex();
			yield return checkEpicycle();

			yield return null;
		}

		IEnumerator checkComplex()
		{
			complex a = new complex(0, 1.5f);
			complex b = new complex(0, 1.5f);
			Debug.Log("eql: " + (a == b).ToString().colorTag("lime"));
			Debug.Log("neql: " + (a != b).ToString().colorTag("orange"));
			Vector2 vec2 = a;
			Debug.Log($"vec2: {vec2}");
			complex c = vec2;
			Debug.Log($"complex: {c}");
			yield return null;
		}

		[SerializeField] Transform _PTr;
		[SerializeField] Transform _tipTr;
		[SerializeField] FourierEpiCycle.Epicycle epicycle;
		[SerializeField] int _freq = 1;
		[SerializeField] int _count = 2;
		[SerializeField] List<FourierEpiCycle.Epicycle> EPICYCLE;

		IEnumerator checkEpicycle()
		{
			Vector2 pathFunc(float t)
			{
				if (t == 1f)
					return _PTr.gcLeaves<Transform>().getAtLast(0).position;

				int index = (t * this._PTr.childCount).floor();
				return _PTr.GetChild(index).position;
			}
			this.epicycle = FourierEpiCycle.GetEpiCycleForFreq(pathFunc: pathFunc, freq: this._freq);

			FourierEpiCycle.InitEpicycles(pathFunc: pathFunc, count: this._count, N: (int)1e2);
			this.EPICYCLE = FourierEpiCycle.EPICYCLE;
			this._tipTr.toggle(true);

			float rotAmount = 0f;
			float speed = 1f / 5;
			for (rotAmount = 0f; rotAmount <= 2f; rotAmount += Time.deltaTime * speed)
			{
				// draw all epicycles >>
				Vector2 sum = Vector2.zero;
				foreach (var epicycle in FourierEpiCycle.EPICYCLE)
				{
					Vector2 newPos = epicycle.GetLocalPosAtRotAmount(rotAmount: rotAmount);
					if (epicycle.freq != 0)
						Line.create(epicycle.freq + "-preview")
							.setA(sum).setN(newPos)
							.setE(1f / 50)
							.setCol(Color.red);
					sum += newPos;
				}
				// << draw all epicycles
				this._tipTr.position = sum;

				yield return new WaitForEndOfFrame();
			}
			yield return null;
		}
	}


	public static class FourierEpiCycle
	{
		[System.Serializable]
		public class Epicycle
		{
			public complex start;
			public int freq = 0;
			public complex GetLocalPosAtRotAmount(float rotAmount = 0f)
			{
				return start * complex.FromPolar(this.freq * 2 * C.pi * rotAmount);
			}
		}

		public static List<Epicycle> EPICYCLE;
		public static void InitEpicycles(Func<float, Vector2> pathFunc, int count = 10, int N = (int)1e3)
		{
			EPICYCLE = new List<Epicycle>();
			for (int i0 = 0; i0 <= count; i0 += 1)
			{
				if (i0 != 0)
				{
					EPICYCLE.Add(GetEpiCycleForFreq(pathFunc: pathFunc, freq: -i0, N: N));
					EPICYCLE.Add(GetEpiCycleForFreq(pathFunc: pathFunc, freq: +i0, N: N));
				}
				else
					EPICYCLE.Add(GetEpiCycleForFreq(pathFunc: pathFunc, freq: i0, N: N));
			}
		}

		// intake float(t) -> return vec2
		// cn = AREA(t -> 0f to 1f), z(t) * polar(-2 * PI * freq * t) * dt
		public static Epicycle GetEpiCycleForFreq(Func<float, Vector2> pathFunc, int freq = 1, int N = (int)1e3) 
		{
			float dt = 1f / N;
			complex sum = new complex(0f, 0f);
			for(int i0 = 0; i0 < N; i0 += 1) // do not include t = 0f, t = 1f if its a loop
			{
				float t = i0 * 1f / N;
				sum += (complex)(pathFunc(t)) * complex.FromPolar(-2 * C.pi * freq * t) * dt;
			}
			return new Epicycle()
			{
				start = sum,
				freq = freq,
			};
		}

	}

	#region complex
	[System.Serializable]
	public struct complex
	{
		public float x, y;

		public complex(float x, float y)
		{
			this.x = x;
			this.y = y;
		}
		public override string ToString()
		{
			// return base.ToString();
			return $"<{this.x}, {this.y}>";
		}

		public static complex FromPolar(float angle, float radius = 1f)
		{
			return new complex(Mathf.Cos(angle),Mathf.Sin(angle)) * radius;
		}

		public float getAngle
		{
			get
			{
				return Mathf.Atan2(this.y, this.x);
			}
		}
		public float getRadius
		{
			get
			{
				return (new Vector2(x, y)).sqrMag().pow(0.5f);
			}
		}

		public static complex operator +(complex a, complex b) { return new complex() { x = a.x + b.x, y = a.y + b.y }; }
		public static complex operator -(complex a, complex b) { return new complex() { x = a.x - b.x, y = a.y - b.y }; }
		public static bool operator ==(complex a, complex b) { return (b.x - a.x).zero(1e-6) && (b.y - a.y).zero(1e-6); }
		public static bool operator !=(complex a, complex b) { return !(a == b); }

		public static complex operator *(complex a, complex b)
		{
			// (a.x + imginary * a.y), (b.x + imaginary * b.y)
			return new complex()
			{
				x = a.x * b.x - a.y * b.y,
				y = a.x * b.y + a.y * b.x,
			};
		}

		public static complex operator *(complex a, float scale)
		{
			// (a.x + imginary * a.y), (b.x + imaginary * b.y)
			return new complex()
			{
				x = a.x * scale,
				y = a.y * scale,
			};
		}
		public static complex operator *(float scale, complex a)
		{
			return a * scale;
		}
		public static complex operator /(complex a, float scale)
		{
			// (a.x + imginary * a.y), (b.x + imaginary * b.y)
			return new complex()
			{
				x = a.x / scale,
				y = a.y / scale,
			};
		}
		public static complex operator /(float scale, complex a)
		{
			return a / scale;
		}

		public static implicit operator complex(Vector2 vec2) { return new complex(vec2.x, vec2.y); }
		public static implicit operator Vector2(complex @this) { return new Vector2(@this.x, @this.y); }
	}
	#endregion
}