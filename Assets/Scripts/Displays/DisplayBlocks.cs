using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DisplayBlocks : MonoBehaviour, IDisplay
{
	private const int WIDTH = 64;
	private const int HEIGHT = 32;

	[SerializeField]
	private GameObject background;
	[SerializeField]
	private Transform pixelParent;

	private List<GameObject> pixelsObjects = new List<GameObject>();

	void Start ()
	{
	
	}

	public void ClearScreen()
	{
		for (int i = 0; i < pixelsObjects.Count; i++)
		{
			pixelsObjects[i].SetActive(false);
		}
	}

	public void CreateScreen()
	{
		Bounds bounds = background.GetComponent<Renderer>().bounds;
		Vector3 topLeft = new Vector3(bounds.min.x + 0.5f, bounds.max.y - 0.5f, gameObject.transform.position.z);
		for (int y = 0; y < HEIGHT; y++)
		{
			for (int x = 0; x < WIDTH; x++)
			{
				GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
				go.transform.position = new Vector3(topLeft.x + x, topLeft.y - y, topLeft.z);
				go.transform.SetParent(pixelParent);
				Rigidbody rb = go.AddComponent<Rigidbody>();
				rb.isKinematic = true;
				pixelsObjects.Add(go);
				go.SetActive(false);
			}
		}
	}

	public void UpdateScreen(BitArray bitData)
	{
		for (int i = 0; i < pixelsObjects.Count; i++)
		{
			pixelsObjects[i].SetActive(bitData[i]);
		}
	}
}
