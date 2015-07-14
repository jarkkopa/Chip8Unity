using UnityEngine;
using System.Collections;

public class DisplayTexture : MonoBehaviour, IDisplay
{
	private const int WIDTH = 64;
	private const int HEIGHT = 32;

	private Renderer meshRenderer;
	private Material material;
	private Texture2D texture;

	void Awake ()
	{
		meshRenderer = gameObject.GetComponent<Renderer>();
		material = meshRenderer.material;
	}

	public void ClearScreen()
	{
		Color[] colors = new Color[WIDTH * HEIGHT];
		for (int i = 0; i < colors.Length; i++)
		{
			colors[i] = Color.black;
		}
		texture.SetPixels(colors);
		texture.Apply();
	}

	public void CreateScreen()
	{
		texture = new Texture2D(WIDTH, HEIGHT, TextureFormat.RGB24, true);
		texture.filterMode = FilterMode.Point;
		texture.Apply();
		material.mainTexture = texture;
	}

	public void UpdateScreen(BitArray pixels)
	{
		Color[] colors = new Color[pixels.Count];
		for(int i=0; i<pixels.Count; i++)
		{
			colors[i] = pixels.Get(i) == true ? Color.white : Color.black;
		}
		texture.SetPixels(colors);
		texture.Apply();
	}
}
