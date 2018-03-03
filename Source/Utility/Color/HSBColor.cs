using UnityEngine;

namespace ironVoxel {
	[System.Serializable]
	public struct HSBColor
	{
	    public float h;
	    public float s;
	    public float b;
	    public float a;
	    
	    public HSBColor(float h, float s, float b, float a)
	    {
	        this.h = h;
	        this.s = s;
	        this.b = b;
	        this.a = a;
	    }
	    
	    public HSBColor(float h, float s, float b)
	    {
	        this.h = h;
	        this.s = s;
	        this.b = b;
	        this.a = 1f;
	    }
	    
	    public HSBColor(Color col)
	    {
	        HSBColor temp = FromColor(col);
	        h = temp.h;
	        s = temp.s;
	        b = temp.b;
	        a = temp.a;
	    }
	    
	    public static HSBColor FromColor(Color color)
	    {
			HSBColor ret;
			ret.a = color.a;
			ret.h = 0.0f;
			ret.s = 0.0f;
			ret.b = 0.0f;
	        
	        float r = color.r;
	        float g = color.g;
	        float b = color.b;
	        
	        float max = Mathf.Max(r, Mathf.Max(g, b));
	        
	        if (max <= 0)
	        {
	            return ret;
	        }
	        
	        float min = Mathf.Min(r, Mathf.Min(g, b));
	        float dif = max - min;
	        
	        if (max > min)
	        {
	            if (g == max)
	            {
	                ret.h = (b - r) / dif * 60f + 120f;
	            }
	            else if (b == max)
	            {
	                ret.h = (r - g) / dif * 60f + 240f;
	            }
	            else if (b > g)
	            {
	                ret.h = (g - b) / dif * 60f + 360f;
	            }
	            else
	            {
	                ret.h = (g - b) / dif * 60f;
	            }
	            if (ret.h < 0)
	            {
	                ret.h = ret.h + 360f;
	            }
	        }
	        else
	        {
	            ret.h = 0;
	        }
	        
	        ret.h *= 1f / 360f;
	        ret.s = (dif / max) * 1f;
	        ret.b = max;
	        
	        return ret;
	    }
	    
	    public static Color ToColor(HSBColor hsbColor)
	    {
	        float r = hsbColor.b;
	        float g = hsbColor.b;
	        float b = hsbColor.b;
	        if (hsbColor.s != 0)
	        {
	            float max = hsbColor.b;
	            float dif = hsbColor.b * hsbColor.s;
	            float min = hsbColor.b - dif;
	            
	            float h = hsbColor.h * 360f;
	            
	            if (h < 60f)
	            {
	                r = max;
	                g = h * dif / 60f + min;
	                b = min;
	            }
	            else if (h < 120f)
	            {
	                r = -(h - 120f) * dif / 60f + min;
	                g = max;
	                b = min;
	            }
	            else if (h < 180f)
	            {
	                r = min;
	                g = max;
	                b = (h - 120f) * dif / 60f + min;
	            }
	            else if (h < 240f)
	            {
	                r = min;
	                g = -(h - 240f) * dif / 60f + min;
	                b = max;
	            }
	            else if (h < 300f)
	            {
	                r = (h - 240f) * dif / 60f + min;
	                g = min;
	                b = max;
	            }
	            else if (h <= 360f)
	            {
	                r = max;
	                g = min;
	                b = -(h - 360f) * dif / 60 + min;
	            }
	            else
	            {
	                r = 0;
	                g = 0;
	                b = 0;
	            }
	        }
	        
			Color color;
			color.a = hsbColor.a;
			color.r = Mathf.Clamp01(r);
			color.g = Mathf.Clamp01(g);
			color.b = Mathf.Clamp01(b);
			return color;
	    }
	    
	    public Color ToColor()
	    {
	        return ToColor(this);
	    }
	    
	    public override string ToString()
	    {
	        return "H:" + h + " S:" + s + " B:" + b;
	    }
	}
}