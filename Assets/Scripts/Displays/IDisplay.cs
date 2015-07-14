using UnityEngine;
using System.Collections;

public interface IDisplay
{
	void ClearScreen();
	void CreateScreen();
	void UpdateScreen(BitArray pixels);
}
