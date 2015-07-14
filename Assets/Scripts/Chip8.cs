using UnityEngine;
using System.Collections;

[System.Serializable]
public class Chip8//: MonoBehaviour
{
#region constants
	/// <summary>
	/// 5 bytes long digits from 0 to F. 8x5 pixels
	/// </summary>
	private readonly byte[] DIGITS = new byte[80]
	{
		0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
		0x20, 0x60, 0x20, 0x20, 0x70, // 1
		0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
		0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
		0x90, 0x90, 0xF0, 0x10, 0x10, // 4
		0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
		0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
		0xF0, 0x10, 0x20, 0x40, 0x40, // 7
		0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
		0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
		0xF0, 0x90, 0xF0, 0x90, 0x90, // A
		0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
		0xF0, 0x80, 0x80, 0x80, 0xF0, // C
		0xE0, 0x90, 0x90, 0x90, 0xE0, // D
		0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
		0xF0, 0x80, 0xF0, 0x80, 0x80  // F
	};
	/// <summary>
	/// Maximum number of cycles keypad input is active before it is read.
	/// Key is reset if it is not read in INPUT_CYCLE_DURATION cycles.
	/// </summary>
	private const byte INPUT_CYCLE_DURATION = 100;
	/// <summary>
	/// Program counter starts at 0x200. It's the start position of most Chip-8 programs.
	/// </summary>
	private const ushort PC_START_POS = 0x200;
	private const ushort SCREEN_HEIGHT = 32;
	private const ushort SCREEN_WIDTH = 64;
#endregion

	/// <summary>
	/// Delay timer register value. Delay timer subtracts 1 when value is non-zero at a rate of 60Hz and deactivates when the value reaches 0.
	/// </summary>
	private byte delayReg;
	/// <summary>
	/// Index register
	/// </summary>
	private ushort indexReg;
	/// <summary>
	/// Hex keypad with keys 0 - F
	/// </summary>
	private byte[] keypad = new byte[16];
	/// <summary>
	/// 4K memory
	/// </summary>
	private byte[] memory = new byte[4096];
	/// <summary>
	/// 2 byte long opcode
	/// </summary>
	private ushort opcode;
	/// <summary>
	/// Program counter
	/// </summary>
	private ushort pc;
	/// <summary>
	/// Screen is refreshed when true
	/// </summary>
	private bool refreshScreen = false;
	/// <summary>
	/// Black and white screen with resolution of 64 x 32 (2048px)
	/// </summary>
	private BitArray screen = new BitArray(SCREEN_WIDTH * SCREEN_HEIGHT);
	/// <summary>
	/// Sound timer register value. Sound timer is active when value is non-zero. Decrements at every cycle.
	/// As long as this value is greater than zero, the chip buzzer will sound.
	/// </summary>
	private byte soundReg;
	/// <summary>
	/// Stack pointer
	/// </summary>
	private ushort sp;
	/// <summary>
	/// Stack used to store the addresses that the interpreter should return to when finished a subroutine.
	/// Up to 16 levels of nested subroutines are supported.
	/// </summary>
	private ushort[] stack = new ushort[16];
	/// <summary>
	/// CPU registers V0...VF
	/// </summary>
	private byte[] v = new byte[16];

	public bool RefreshScreen { get { return refreshScreen; } set { refreshScreen = value; } }
	public BitArray Screen { get { return screen; } }
	public byte SoundReg { get { return soundReg; } }

	public Chip8()
	{
		Initialize();
	}

	/// <summary>
	/// Loads data into programs memory. Memory starts from PC_START_POS (0x200)
	/// </summary>
	/// <param name="data"></param>
	public void LoadIntoMemory(byte[] data)
	{
		for (int i = 0; i < data.Length; i++)
		{
			memory[i + PC_START_POS] = data[i];
		}
	}

	public void Initialize()
	{
		pc = PC_START_POS;
		opcode = 0;
		indexReg = 0;
		sp = 0;

		//Load digits to memory 0x000 to 0x1FF
		for (int i = 0; i < DIGITS.Length; i++)
		{
			memory[i] = DIGITS[i];
		}

		//Reset registers
		for (int i = 0; i < v.Length; i++)
		{
			v[i] = 0;
		}

		//Reset keypad
		for (int i = 0; i < keypad.Length; i++)
		{
			keypad[i] = 0;
		}

		//Reset call stack
		for (int i = 0; i < stack.Length; i++)
		{
			stack[i] = 0;
		}

		//Reset screen
		for (int i = 0; i < screen.Count; i++)
		{
			screen[i] = false;
		}
		refreshScreen = true;

		//Reset timers
		delayReg = 0;
		soundReg = 0;
	}

	public void SetKeys(BitArray keys)
	{
		//for (int i = 0; i < keypad.Count; i++)
		for (int i = 0; i < keypad.Length; i++)
		{
			//Change value only when it's set to true. Keys are reset manually in RunCycle when a keypress is handled.
			if(keys[i] == true)
				keypad[i] = INPUT_CYCLE_DURATION;
		}
	}

	public void RunCycle()
	{
		/* Opcode is 16-bit long. Fetch the data pointed by the pc and merge it with the next byte.
		* In C# the second shift operand is always 32-bit int and therefore the result will also be a 32-bit int.
		* The result can be casted to ushort however because the data is shifted only 8 bits and therefore the result should never exceed 16 bits.
		*/
		opcode = (ushort)(memory[pc] << 8 | memory[pc + 1]);

		//Decode and execute opcode
		switch(opcode & 0xF000)
		{
			case 0x0000: // 0x0???
				switch(opcode & 0x0FFF)
				{
					//00E0 CLS - Clear the display
					case 0x00E0:
						for (int i = 0; i < screen.Length; i++)
						{
							screen[i] = false;
						}
						pc += 2;
						refreshScreen = true;
						break;

					//00EE RET - Return from a subroutine
					case 0x00EE:
						sp--;
						pc = stack[sp];
						pc += 2;
						break;
					default:
						Debug.LogError("Error! Unknown opcode: " + opcode.ToString("x2"));
						break;
				}
				break;

			case 0x1000: // 0x1???
				//1nnn - JP addr - Jump to location nnn.
				pc = (ushort)(opcode & 0x0FFF);
				break;

			case 0x2000: // 0x2???
				//2nnn - CALL addr - Call subroutine at nnn.
				stack[sp] = pc;
				sp++;
				pc = (ushort)(opcode & 0x0FFF);
				break;

			case 0x3000: // 0x3???
				//3xkk - SE Vx, byte - Skip next instruction if Vx = kk.
				//Shift 8 bits to right because there's 8 bits (kk) after x
				if (v[(opcode & 0x0F00) >> 8] == (opcode & 0x00FF))
					pc += 4;
				else
					pc += 2;
				break;

			case 0x4000: // 0x4???
				//4xkk - SNE Vx, byte - Skip next instruction if Vx != kk.
				if (v[(opcode & 0x0F00) >> 8] != (opcode & 0x0FF))
					pc += 4;
				else
					pc += 2;
				break;

			case 0x5000: // 0x5???
				//5xy0 - SE Vx, Vy - Skip next instruction if Vx = Vy.
				if ((opcode & 0x000F) == 0x0000)
				{
					if (v[(opcode & 0x0F00) >> 8] == v[(opcode & 0x00F0) >> 4])
						pc += 4;
					else
						pc += 2;
				}
				else
					Debug.LogError("Error! Unknown opcode: " + opcode.ToString("x2"));
				break;

			case 0x6000: // 0x6???
				//6xkk - LD Vx, byte - Set Vx = kk.
				v[(opcode & 0x0F00) >> 8] = (byte)(opcode & 0x00FF);
				pc += 2;
				break;

			case 0x7000: // 0x7???
				//7xkk - ADD Vx, byte - Set Vx = Vx + kk.
				v[(opcode & 0x0F00) >> 8] = (byte)(v[(opcode & 0x0F00) >> 8] + opcode & 0x00FF);
				pc += 2;
				break;

			case 0x8000: // 0x8???
				switch(opcode & 0x000F)
				{
					case 0x0000:
						//8xy0 - LD Vx, Vy - Set Vx = Vy.
						v[(opcode & 0x0F00) >> 8] = v[(opcode & 0x00F0) >> 4];
						pc += 2;
						break;

					case 0x0001:
						//8xy1 - OR Vx, Vy - Set Vx = Vx OR Vy.
						v[(opcode & 0x0F00) >> 8] = (byte)(v[(opcode & 0x0F00) >> 8] | v[(opcode & 0x00F0) >> 4]);
						pc += 2;
						break;

					case 0x0002:
						//8xy2 - AND Vx, Vy - Set Vx = Vx AND Vy.
						v[(opcode & 0x0F00) >> 8] = (byte)(v[(opcode & 0x0F00) >> 8] & v[(opcode & 0x00F0) >> 4]);
						pc += 2;
						break;

					case 0x0003:
						//8xy3 - XOR Vx, Vy - Set Vx = Vx XOR Vy.
						v[(opcode & 0x0F00) >> 8] = (byte)(v[(opcode & 0x0F00) >> 8] ^ v[(opcode & 0x00F0) >> 4]);
						pc += 2;
						break;
                        
					case 0x0004:
						//8xy4 - ADD Vx, Vy - Set Vx = Vx + Vy, set VF = carry.
						int temp = v[(opcode & 0x0F00) >> 8] + v[(opcode & 0x00F0) >> 4];
						//Set carry flag is result is greater than 8 bits
						if (((temp >> 8) & 0xFFFF) >= 1)
							v[0xF] = 1;
						else
							v[0xF] = 0;
						//Only the lowest 8 bit are stored in v[x]
						v[(opcode & 0x0F00) >> 8] = (byte)temp;
						pc += 2;
						break;

					case 0x0005:
						//8xy5 - SUB Vx, Vy - Set Vx = Vx - Vy, set VF = NOT borrow.
						if (v[(opcode & 0x0F00) >> 8] > v[(opcode & 0x00F0) >> 4])
							v[0xF] = 1;
						else
							v[0xF] = 0;

						v[(opcode & 0x0F00) >> 8] = (byte)(v[(opcode & 0x0F00) >> 8] - v[(opcode & 0x00F0) >> 4]);
						pc += 2;
						break;

					case 0x0006:
						//8xy6 - SHR Vx {, Vy} - Set Vx = Vx SHR 1.
						//Set v[F] to 1 if the least significant in v[x] bit is 1, otherwise 0.
						v[0xF] = (byte)(v[(opcode & 0x0F00) >> 8] & 0x1);
						//Right shift v[x]
						v[(opcode & 0x0F00) >> 8] = (byte)(v[(opcode & 0x0F00) >> 8] >> 1);
						pc += 2;
						break;

					case 0x0007:
						//8xy7 - SUBN Vx, Vy - Set Vx = Vy - Vx, set VF = NOT borrow.
						if (v[(opcode & 0x00F0) >> 4] > v[(opcode & 0x0F00) >> 8])
							v[0xF] = 1;
						else
							v[0xF] = 0;

						v[(opcode & 0x0F00) >> 8] = (byte)(v[(opcode & 0x00F0) >> 4] - v[(opcode & 0x0F00) >> 8]);
						pc += 2;
						break;

					case 0x000E:
						//8xyE - SHL Vx {, Vy} - Set Vx = Vx SHL 1.
						//Set v[F] = 1 if the least significant bit in v[x] is 1, otherwise 0
						v[0xF] = (byte)(v[(opcode & 0x0F00) >> 8] & 0x1);
						//Left shift v[x]
						v[(opcode & 0x0F00) >> 8] = (byte)(v[(opcode & 0x0F00) >> 8] >> 1);
						pc += 2;
						break;

					default:
						Debug.LogError("Error! Unknown opcode: " + opcode.ToString("x2"));
						break;
				}
				break;

			case 0x9000: // 0x9???
				//9xy0 - SNE Vx, Vy - Skip next instruction if Vx != Vy.
				if ((opcode & 0x000F) == 0x0000)
				{
					if (v[(opcode & 0x0F00) >> 8] != v[(opcode & 0x00F0) >> 4])
						pc += 4;
					else
						pc += 2;
				}
				else
					Debug.LogError("Error! Unknown opcode: " + opcode.ToString("x2"));
				break;

			case 0xA000: // 0xA???
				//Annn - LD I, addr - Set I = nnn.
				indexReg = (ushort)(opcode & 0x0FFF);
				pc += 2;
				break;

			case 0xB000: // 0xB???
				//Bnnn - JP V0, addr - Jump to location nnn + V0.
				pc = (ushort)((opcode & 0x0FFF) + v[0]);
				break;

			case 0xC000: // 0xC???
				//Cxkk - RND Vx, byte - Set Vx = random byte AND kk.
				//Random.Next is inclusive, exclusive
				v[(opcode & 0x0F00) >> 8] = (byte)((opcode & 0x00FF) & Random.Range(0x0, 0x100));
				pc += 2;
				break;

			case 0xD000:// 0xD???
				//Dxyn - DRW Vx, Vy, nibble - Display n-byte sprite starting at memory location I at (Vx, Vy), set VF = collision.
				ushort length = (ushort)(opcode & 0x000F);
				ushort x = v[(ushort)((opcode & 0x0F00) >> 8)];
				ushort y = v[(ushort)((opcode & 0x00F0) >> 4)];

				//Wrap sprites if they are completely outside the screen
				x = (ushort)(x % SCREEN_WIDTH);
				y = (ushort)(y % SCREEN_HEIGHT);

				ushort sprite;
				v[0xF] = 0;

				//Cut of if part of sprite goes offscreen
				int rowMax = length;
				if (y + length > SCREEN_HEIGHT)
					rowMax = SCREEN_HEIGHT - y;
				//Loop over rows
				for (int row = 0; row < rowMax; row++)
				{
					//Read sprite from memory
					sprite = memory[indexReg + row];
					//Screen bit position for x, y + row
					int index = (y + row) * SCREEN_WIDTH + x;

					//Cut of if part of sprite goes offscreen
					int xMax = 8;
					if (x + 8 > SCREEN_WIDTH)
						xMax = SCREEN_WIDTH - x;

					for (int xBit = 0; xBit < xMax; xBit++)
					{
						if ((sprite & 0x80 >> xBit) != 0)
						{
							int tempIndex = index + xBit;
							bool value = (sprite & 0x80 >> xBit) != 0;
							//Set collision flag if this pixel was already set
							if(screen[tempIndex] == true)
								v[0xF] = 1;
							screen[tempIndex] ^= value;
						}
					}
				}

				refreshScreen = true;
				pc += 2;
				break;

			case 0xE000:// 0xE???
				if ((opcode & 0x00FF) == 0x009E)
				{
					//Ex9E - SKP Vx - Skip next instruction if key with the value of Vx is pressed.
					if (keypad[v[(opcode & 0x0F00) >> 8]] > 0)
					{
						//Reset key
						keypad[v[(opcode & 0x0F00) >> 8]] = 0;
						pc += 4;
					}
					else
						pc += 2;
				}
				else if ((opcode & 0x00FF) == 0x00A1)
				{
					//ExA1 - SKNP Vx - Skip next instruction if key with the value of Vx is not pressed.
					if (keypad[v[(opcode & 0x0F00) >> 8]] == 0)
					{
						pc += 4;
					}
					else
					{
						//Reset key
						keypad[v[(opcode & 0x0F00) >> 8]] = 0;
						pc += 2;
					}
				}
				else
					Debug.LogError("Error! Unknown opcode: " + opcode.ToString("x2"));
				break;

			case 0xF000:// 0xF???
				switch(opcode & 0x00FF)
				{
					case 0x0007:
						//Fx07 - LD Vx, DT - Set Vx = delay timer value.
						v[(opcode & 0x0F00) >> 8] = delayReg;
						pc += 2;
						break;

					case 0x000A:
						//Fx0A - LD Vx, K - Wait for a key press, store the value of the key in Vx.
						bool keyPressed = false;
						for (byte i = 0; i < keypad.Length; i++)
						{
							if (keypad[i] > 0)
							{
								keyPressed = true;
								//Reset key
								keypad[i] = 0;
								v[(opcode & 0x0F00) >> 8] = i;
								break;
							}
						}
						//Wait for key press
						if (keyPressed == false)
							break;
						else
							pc += 2;
						break;

					case 0x0015:
						//Fx15 - LD DT, Vx - Set delay timer = Vx.
						delayReg = v[(opcode & 0x0F00) >> 8];
						pc += 2;
						break;

					case 0x0018:
						//Fx18 - LD ST, Vx - Set sound timer = Vx.
						soundReg = v[(opcode & 0x0F00) >> 8];
						pc += 2;
						break;

					case 0x001E:
						//Fx1E - ADD I, Vx - Set I = I + Vx.
						indexReg = (ushort)(indexReg + v[(opcode & 0x0F00) >> 8]);
						//VF is set to 1 when range overflow (I+VX>0xFFF), and 0 when there isn't.
						//This is undocumented feature of the CHIP-8 and used by Spacefight 2091! game. Source: https://en.wikipedia.org/wiki/CHIP-8
						if (indexReg > 0xFFF)
							v[0xF] = 1;
						else
							v[0xF] = 0;
						pc += 2;
						break;

					case 0x0029:
						//Fx29 - LD F, Vx - Set I = location of sprite for digit Vx.
						indexReg = (ushort)(5 * v[(opcode & 0x0F00) >> 8]);
						pc += 2;
						break;

					case 0x0033:
						//Fx33 - LD B, Vx - Store BCD representation of Vx in memory locations I, I+1, and I+2.
						byte value = v[(opcode & 0x0F00) >> 8];
						memory[indexReg] = (byte)(value / 100);
						memory[indexReg + 1] = (byte)((value / 10) % 10);
						memory[indexReg + 2] = (byte)((value % 100) % 10);
						pc += 2;
						break;

					case 0x0055:
						//Fx55 - LD [I], Vx - Store registers V0 through Vx in memory starting at location I.
						for (int i = 0; i <= ((opcode & 0x0F00) >> 8); i++)
						{
							memory[indexReg + i] = v[i];
						}
						pc += 2;
						break;

					case 0x0065:
						//Fx65 - LD Vx, [I] - Read registers V0 through Vx from memory starting at location I.
						for (int i = 0; i <= ((opcode & 0x0F00) >> 8); i++)
						{
							v[i] = memory[indexReg + i];
						}
						pc += 2;
						break;

					default:
						Debug.LogError("Error! Unknown opcode: " + opcode.ToString("x2"));
						break;
				}
				break;

			default:
				Debug.LogError("Error! Unknown opcode: " + opcode.ToString("x2"));
				break;
		}

		//Update timers
		if (delayReg > 0)
		{
			delayReg--;
		}

		if(soundReg > 0)
		{
			soundReg--;
		}

		ReduceKeypadCycles();
	}

	/// <summary>
	/// Reduces positive keypad values by one.
	/// Used for ignoring inputs that are set more than INPUT_CYCLE_DURATION cycles ago and not yet handled.
	/// </summary>
	private void ReduceKeypadCycles()
	{
		for (int i = 0; i < keypad.Length; i++)
		{
			if (keypad[i] > 0)
				keypad[i]--;
		}
	}
}
