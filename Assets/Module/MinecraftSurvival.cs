using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rand = UnityEngine.Random;

public class MinecraftSurvival : MonoBehaviour {

	// Bomb Vars
	public KMBombInfo bomb;
	public KMAudio bAudio;

	public Material[] _dimensionMaterials;
	public Material[] _swordMaterials;
	public Material[] _mobMaterials;

	public KMSelectable[] _actionButtons; // Inventory and dimensions and fighting
	public KMSelectable[] _resourceButtons; // Resource buttons
	public KMSelectable[] _inventoryButtons; // Inventory/crafting buttons
	public KMSelectable[] _allResourceInventoryIndicies; // Contains all resource indicies for the array
	public KMSelectable _module;

	public SpriteRenderer[] _fullHunger;
	public SpriteRenderer[] _emptyHunger;
	public SpriteRenderer[] _fullHearts;
	public SpriteRenderer[] _halfHearts;
	public SpriteRenderer[] _emptyHearts;
	public SpriteRenderer[] _regenHearts;

	public MeshRenderer[] _actionButtonMeshes;
	public MeshRenderer[] _resourceButtonMeshes;
	public MeshRenderer[] _inventoryButtonMeshes;
	public MeshRenderer[] _moduleMeshes;
	public MeshRenderer _mobIndicator;

	public TextMesh _materialValueText;
	public TextMesh _mobHealthIndicator;

	// Specifically for Logging
	static int _modIDCount = 0;
	int _modID;
	private bool _modSolved;

	// Module Vars
	int _gMonsterIndex = 0;
	int _gMonsterHealth = 0;
	int _gMonsterDamage = 0;
	int _dimensionIndex = 0;
	int _resourceUntilFight = 0; // Number of resource pick ups before
	int _playerHealth = 0;
	int _playerDamage = 0;
	int _playerProc = 0;
	int _playerHunger = 0;

	int[] _monsterHealth = new int[] { 30, 20, 15, 20, 20, 40, 30, 40, 50, 30, 500 };
	int[] _monsterDamage = new int[] { 6, 4, 4, 20, 4, 6, 5, 8, 10, 5, 8 };
	int[] _materialValues = new int[44];
	int[] _swordDamages = new int[] { 6, 9, 12, 15 };
	int[] _armorProc = new int[] { 4, 8 };

	string _gMonsterName = "";

	string[] _monsterIndex = new string[] { "Zombie", "Skeleton", "Spider", "Creeper", "Slime", "Pigman", "Blaze", "Wither Skeleton", "Enderman", "Shulker", "Ender Dragon" };
	string[][] _monsterTable = new string[][] {
		new string[] { "Zombie", "Skeleton", "Spider", "Creeper", "Slime", "Enderman" },
		new string[] { "Skeleton", "Pigman", "Blaze", "Wither Skeleton", "Enderman" },
		new string[] { "Enderman", "Shulker" }
	};

	bool _isNetherUnlocked = false;
	bool _isEndUnlocked = false;
	bool _fightStarted = false;
	bool _dragonFightStarted = false;
	bool _isAnimating = false;
	bool _inInventory = false;
	bool _fightReset = false;
	bool _dragonDefeated = false;

	void Awake() {
		_modID = _modIDCount++;

		for (int i = 0; i < _materialValues.Length; i++) {
			_materialValues[i] = 0;
		}

		foreach (KMSelectable action in _actionButtons) {
			action.OnInteract += delegate () { if (_modSolved) { return false; } ActionButton(action); return false; };
		}

		foreach (KMSelectable resource in _resourceButtons) {
			
			resource.OnInteract += delegate () { if (_modSolved) { return false; } ResourceButton(resource); return false; };
		}

		foreach (KMSelectable inv in _allResourceInventoryIndicies) {
			
			inv.OnInteract += delegate () { if (_modSolved) { return false; } InventoryButton(inv); return false; };
			inv.OnHighlight += delegate () { if (_isAnimating) { return; } UpdateAmountDisplay(inv); return; };
			inv.OnHighlightEnded += delegate () { _materialValueText.text = ""; return;  };
		}
		_resourceUntilFight = rand.Range(6,13);
	}

	void Start() {
		StartCoroutine(UpdateHighlights());
		StartCoroutine(UpHunger());
	}

	void ActionButton(KMSelectable but) {
		int index = Array.IndexOf(_actionButtons, but);
		switch (index) {
			case 0:
				if (_dimensionIndex == 0) {
					break;
				}
				_dimensionIndex = 0;
				UpdateModule();
				break;
			case 1:
				if (_dimensionIndex == 1) {
					break;
				}
				if (!_isNetherUnlocked && _materialValues[25] >= 14 && _materialValues[43] >= 1)
				{
					_materialValues[25] -= 12;
					_materialValues[43]--;
					_isNetherUnlocked = true;
					_dimensionIndex = 1;
					UpdateModule();
					Debug.LogFormat("[Minecraft Survival #{0}]: Unlocked 'The Nether'.",_modID);
				}
				if (!_isNetherUnlocked) {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! Tried to access The Nether without it being unlocked.", _modID);
					break;
				}
				_dimensionIndex = 1;
				UpdateModule();
				break;
			case 2:
				if (_dimensionIndex == 2) {
					break;
				}
				if (!_isEndUnlocked && _materialValues[18] >= 12)
				{
					_materialValues[18] -= 12;
					_isEndUnlocked = true;
					_dimensionIndex = 2;
					UpdateModule();
					Debug.LogFormat("[Minecraft Survival #{0}]: Unlocked 'The End'.", _modID);
				}
				if (!_isEndUnlocked)
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! Tried to access The End without it being unlocked.", _modID);
					break;
				}
				_dimensionIndex = 2;
				UpdateModule();
				break;
			case 3:
				if (_fightStarted && !_isAnimating)
				{
					FightEventHandler();
				}
				break;
			case 4:
				if (_fightStarted && !_isAnimating) {
					EatEventHandler();
				}
				break;
			case 5:
				if (_isAnimating) {
					return;
				}
				if (_inInventory) {
					StartCoroutine(CloseInventory());
					_inInventory = false;
					return;
				}
				_inInventory = true;
				StartCoroutine(OpenInventory());
				break;
			case 6:
				if (_isAnimating) { return; }
				EatEventHandler();
				break;
			case 7:
				if (!_dragonDefeated) {
					StartTheDamnDragonFightAlready();
				}
				break;
			default:
				Debug.LogFormat("[Minecraft Survival #{0}]: Unable to change the dimension of the module.",_modID);
				break;
		}
	}

	void ResourceButton(KMSelectable but) {
		_playerHunger--;
		UpdateHunger();
		if (_playerHunger == 0) {
			GetComponent<KMBombModule>().HandleStrike();
			Debug.LogFormat("[Minecraft Survival #{0}]: Strike! You didnt have enough hunger to obtain resources.", _modID);
			_playerHunger = 10;
			UpdateHunger();
			return;
		}
		// Overworld: 42 19 20 21 22 23 25 24
		// Nether: 34 26 27 28
		// End: 29 25 30 31
		int index = Array.IndexOf(_resourceButtons, but);
		switch (index) {
			// Overworld
			case 0:
				if (!(_materialValues[9] >= 1)) {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! There wasn't enough required.",_modID);
					break;
				}
				if (_materialValues[42] >= 64) { break; }
				_materialValues[42] += 2;
				break;
			case 1:
				if (!(_materialValues[5] >= 1 || _materialValues[6] >= 1 || _materialValues[7] >= 1 || _materialValues[8] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! There wasn't enough required.", _modID);
					break;
				}
				if (_materialValues[19] >= 64) { break; }
				_materialValues[19] += rand.Range(1, 4);
				break;
			case 2:
				if (!(_materialValues[5] >= 1 || _materialValues[6] >= 1 || _materialValues[7] >= 1 || _materialValues[8] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! There wasn't enough required.", _modID);
					break;
				}
				if (_materialValues[20] >= 64) { break; }
				_materialValues[20] += rand.Range(3, 6);
				break;
			case 3:
				if (_materialValues[5] >= 0 && !(_materialValues[6] >= 1 || _materialValues[7] >= 1 || _materialValues[8] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! There wasn't enough required.", _modID);
					break;
				}
				if (_materialValues[21] >= 64) { break; }
				_materialValues[21] += rand.Range(1, 4);
				break;
			case 4:
				if ((_materialValues[5] >= 0 && _materialValues[6] >= 0) && !(_materialValues[7] >= 1 || _materialValues[8] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! There wasn't enough required.", _modID);
					break;
				}
				if (_materialValues[22] >= 64) { break; }
				_materialValues[22] += rand.Range(1, 3);
				break;
			case 5:
				if (!(_materialValues[10] >= 1)) {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! There wasn't enough required.", _modID);
					break;
				}
				if (_materialValues[23] >= 64) { break; }
				_materialValues[23]++;
				break;
			case 6:
				if ((_materialValues[5] >= 0 && _materialValues[6] >= 0 && _materialValues[7] >= 0) && !(_materialValues[8] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! There wasn't enough required.", _modID);
					break;
				}
				if (_materialValues[25] >= 64) { break; }
				_materialValues[25] += rand.Range(1, 3);
				break;
			case 7:
				if (!(_materialValues[5] >= 1 || _materialValues[6] >= 1 || _materialValues[7] >= 1 || _materialValues[8] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! There wasn't enough required.", _modID);
					break;
				}
				if (_materialValues[24] >= 64) { break; }
				_materialValues[24] += rand.Range(1,4);
				break;
			// Nether
			case 8:
				if (!(_materialValues[5] >= 1 || _materialValues[6] >= 1 || _materialValues[7] >= 1 || _materialValues[8] >= 1)) {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! There wasn't enough required.", _modID);
					break;
				}
				if (_materialValues[34] >= 64) { break; }
				_materialValues[34] += rand.Range(2, 5);
				break;
			case 9:
				if ((_materialValues[5] >= 0 && _materialValues[6] >= 0) && !(_materialValues[7] >= 1 || _materialValues[8] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! There wasn't enough required.", _modID);
					break;
				}
				if (_materialValues[26] >= 64) { break; }
				_materialValues[26] += 2;
				break;
			case 10:
				if ((_materialValues[5] >= 0 && _materialValues[6] >= 0) && !(_materialValues[7] >= 1 || _materialValues[8] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! There wasn't enough required.", _modID);
					break;
				}
				if (_materialValues[27] >= 64) { break; }
				_materialValues[27] += 2;
				break;
			case 11:
				if (!(_materialValues[10] >= 1)) {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! There wasn't enough required.", _modID);
					break;
				}
				if (_materialValues[28] >= 64) { break; }
				_materialValues[28] += 2;
				break;
			// End
			case 12:
				if (!(_materialValues[5] >= 1 || _materialValues[6] >= 1 || _materialValues[7] >= 1 || _materialValues[8] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! There wasn't enough required.", _modID);
					break;
				}
				if (_materialValues[29] >= 64) { break; }
				_materialValues[29] += rand.Range(2, 4);
				break;
			case 13:
				if ((_materialValues[5] >= 0 && _materialValues[6] >= 0 && _materialValues[7] >= 0) && !(_materialValues[8] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! There wasn't enough required.", _modID);
					break;
				}
				if (_materialValues[25] >= 64) { break; }
				_materialValues[25] += rand.Range(2, 4);
				break;
			case 14:
				if (!(_materialValues[5] >= 1 || _materialValues[6] >= 1 || _materialValues[7] >= 1 || _materialValues[8] >= 1))
				{
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! There wasn't enough required.", _modID);
					break;
				}
				if (_materialValues[30] >= 64) { break; }
				_materialValues[30] += rand.Range(3, 5);
				break;
			case 15:
				if (!(_materialValues[10] >= 1)) {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! There wasn't enough required.", _modID);
					break;
				}
				if (_materialValues[31] >= 64) { break; }
				_materialValues[31] += 2;
				break;
			default:
				Debug.LogFormat("[Minecraft Survival {0}]: Unable to update resources.",_modID);
				break;
		}
		_resourceUntilFight--;
		if (_resourceUntilFight == 0) {
			StartFightEventHandler();
		}
	}

	void InventoryButton(KMSelectable but) {
		if (_isAnimating) { return; }
		int index = Array.IndexOf(_allResourceInventoryIndicies,but);
		if (!index.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 41, 43)) { return; }
		switch (index) {
			case 0:
				if (_materialValues[42] >= 1) {
					if (_materialValues[0] >= 64) { break; }
					_materialValues[42]--;
					_materialValues[0] += 4;
					UpdateText(0);
					break;
				} else {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! Insufficient materials to craft that item.", _modID);
					break;
				}
			case 1:
				if (_materialValues[0] >= 2) {
					if (_materialValues[1] >= 64) { break; }
					_materialValues[0] -= 2;
					_materialValues[1] += 4;
					UpdateText(1);
					break;
				} else {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! Insufficient materials to craft that item.", _modID);
					break;
				}
			case 2:
				if (_materialValues[19] >= 8) {
					if (_materialValues[2] >= 64) { break; }
					_materialValues[19] -= 8;
					_materialValues[2]++;
					UpdateText(2);
					break;
				} else {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! Insufficient materials to craft that item.", _modID);
					break;
				}
			case 3:
				if (_materialValues[2] >= 1 && _materialValues[21] >= 1 && _materialValues[20] >= 1) {
					if (_materialValues[3] >= 64) { break; }
					_materialValues[21]--;
					_materialValues[20]--;
					_materialValues[3] += rand.Range(1,4);
					UpdateText(3);
					break;
				} else {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! Insufficient materials to craft that item.", _modID);
					break;
				}
			case 4:
				if (_materialValues[2] >= 1 && _materialValues[24] >= 1 && _materialValues[20] >= 1) {
					if (_materialValues[4] >= 64) { break; }
					_materialValues[24]--;
					_materialValues[20]--;
					_materialValues[4] += rand.Range(1,4);
					UpdateText(4);
					break;
				} else {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! Insufficient materials to craft that item.", _modID);
					break;
				}
			case 5:
				if (_materialValues[0] >= 3 && _materialValues[1] >= 2) {
					if (_materialValues[5] >= 64) { break; }
					_materialValues[0] -= 3;
					_materialValues[1] -= 2;
					_materialValues[5]++;
					UpdateText(5);
					break;
				} else {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! Insufficient materials to craft that item.", _modID);
					break;
				}
			case 6:
				if (_materialValues[5] >= 1 && _materialValues[19] >= 4) {
					if (_materialValues[6] >= 64) { break; }
					_materialValues[5]--;
					_materialValues[19] -= 4;
					_materialValues[6]++;
					UpdateText(6);
					break;
				} else {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! Insufficient materials to craft that item.", _modID);
					break;
				}
			case 7:
				if (_materialValues[6] >= 1 && _materialValues[3] >= 5) {
					if (_materialValues[7] >= 64) { break; }
					_materialValues[6]--;
					_materialValues[3] -= 5;
					_materialValues[7]++;
					UpdateText(7);
					break;
				} else {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! Insufficient materials to craft that item.", _modID);
					break;
				}
			case 8:
				if (_materialValues[7] >= 1 && _materialValues[22] >= 6) {
					if (_materialValues[8] >= 64) { break; }
					_materialValues[7]--;
					_materialValues[22] -= 6;
					_materialValues[8]++;
					UpdateText(8);
					break;
				} else {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! Insufficient materials to craft that item.", _modID);
					break;
				}
			case 9:
				if (_materialValues[0] >= 3 && _materialValues[1] >= 2) {
					if (_materialValues[9] >= 64) { break; }
					_materialValues[0] -= 3;
					_materialValues[1] -= 2;
					_materialValues[9]++;
					UpdateText(9);
					break;
				} else {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! Insufficient materials to craft that item.", _modID);
					break;
				}
			case 10:
				if (_materialValues[0] >= 1 && _materialValues[1] >= 2) {
					if (_materialValues[10] >= 64) { break; }
					_materialValues[0]--;
					_materialValues[1] -= 2;
					_materialValues[10]++;
					UpdateText(10);
					break;
				} else {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! Insufficient materials to craft that item.", _modID);
					break;
				}
			case 11:
				if (_materialValues[0] >= 2 && _materialValues[1] >= 1) {
					if (_materialValues[11] >= 64) { break; }
					_materialValues[0] -= 2;
					_materialValues[1]--;
					_materialValues[11]++;
					UpdateText(11);
					break;
				} else {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! Insufficient materials to craft that item.", _modID);
					break;
				}
			case 12:
				if (_materialValues[11] >= 1 && _materialValues[19] >= 4) {
					if (_materialValues[12] >= 64) { break; }
					_materialValues[11]--;
					_materialValues[19] -= 4;
					_materialValues[12]++;
					UpdateText(12);
					break;
				} else {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! Insufficient materials to craft that item.", _modID);
					break;
				}
			case 13:
				if (_materialValues[12] >= 1 && _materialValues[3] >= 5) {
					if (_materialValues[13] >= 64) { break; }
					_materialValues[12]--;
					_materialValues[3] -= 5;
					_materialValues[13]++;
					UpdateText(13);
					break;
				} else {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! Insufficient materials to craft that item.", _modID);
					break;
				}
			case 14:
				if (_materialValues[13] >= 1 && _materialValues[22] >= 6) {
					if (_materialValues[14] >= 64) { break; }
					_materialValues[13]--;
					_materialValues[22] -= 6;
					_materialValues[14]++;
					UpdateText(14);
					break;
				} else {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! Insufficient materials to craft that item.", _modID);
					break;
				}
			case 15:
				if (_materialValues[3] >= 24) {
					if (_materialValues[15] >= 64) { break; }
					_materialValues[3] -= 24;
					_materialValues[15]++;
					UpdateText(15);
					break;
				} else {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! Insufficient materials to craft that item.", _modID);
					break;
				}
			case 16:
				if (_materialValues[22] >= 24) {
					if (_materialValues[16] >= 64) { break; }
					_materialValues[22] -= 24;
					_materialValues[16]++;
					UpdateText(16);
					break;
				} else {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! Insufficient materials to craft that item.", _modID);
					break;
				}
			case 17:
				if (_materialValues[32] >= 1) {
					if (_materialValues[17] >= 64) { break; }
					_materialValues[32]--;
					_materialValues[17] += 2;
					UpdateText(17);
					break;
				} else {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! Insufficient materials to craft that item.", _modID);
					break;
				}
			case 18:
				if (_materialValues[17] >= 1 && _materialValues[33] >= 1) {
					if (_materialValues[18] >= 64) { break; }
					_materialValues[17]--;
					_materialValues[33]--;
					_materialValues[18]++;
					UpdateText(18);
					break;
				} else {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! Insufficient materials to craft that item.", _modID);
					break;
				}
			case 41:
				if (_materialValues[41] >= 1 && _dragonDefeated) {
					SolveModule();
					Debug.LogFormat("[Minecraft Survival #{0}]: Module solved. Have a good time in your world from now on.", _modID);
					GetComponent<KMBombModule>().HandlePass();
					break;
				} else {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! Insufficient materials to craft that item.", _modID);
					break;
				}
			case 43:
				if (_materialValues[3] >= 1 && _materialValues[23] >= 1) {
					if (_materialValues[43] >= 64) { break; }
					_materialValues[3]--;
					_materialValues[23]--;
					_materialValues[43]++;
					UpdateText(43);
					break;
				} else {
					GetComponent<KMBombModule>().HandleStrike();
					Debug.LogFormat("[Minecraft Survival #{0}]: Strike! Insufficient materials to craft that item.", _modID);
					break;
				}
			default:
				Debug.LogFormat("[Minecraft Survival #{0}]: Unable to find the index of the item.",_modID);
				break;
		}
	}

	void UpdateText(int index) {
		_materialValueText.text = _materialValues[index].ToString();
	}

	void UpdateAmountDisplay(KMSelectable but) {
		int index = Array.IndexOf(_allResourceInventoryIndicies, but);
		_materialValueText.text = _materialValues[index].ToString();
	}

	void StartFightEventHandler() {
		if ((_materialValues[11] == 0 || _materialValues[12] == 0 || _materialValues[13] == 0 || _materialValues[14] == 0) && !(_materialValues[11] >= 1 || _materialValues[12] >= 1 || _materialValues[13] >= 1 || _materialValues[14] >= 1)) {
			Debug.LogFormat("[Minecraft Survival #{0}]: Fight did not start because you have no sword!", _modID);
			_resourceUntilFight = rand.Range(2, 7);
			return;
		}
		if (_dragonFightStarted) {
			foreach (KMSelectable km in _resourceButtons) {
				km.GetComponent<Renderer>().enabled = false;
				if (km.Highlight.gameObject.activeSelf) {
					km.Highlight.gameObject.SetActive(false);
				}
			}
			foreach (KMSelectable km in _actionButtons) {
				km.GetComponent<Renderer>().enabled = false;
				if (km.Highlight.gameObject.activeSelf) {
					km.Highlight.gameObject.SetActive(false);
				}
			}
			if (_materialValues[14] >= 1) {
				_actionButtonMeshes[3].material = _swordMaterials[3];
			} else if (_materialValues[13] >= 1) {
				_actionButtonMeshes[3].material = _swordMaterials[2];
			} else if (_materialValues[12] >= 1) {
				_actionButtonMeshes[3].material = _swordMaterials[1];
			} else if (_materialValues[11] >= 1) {
				_actionButtonMeshes[3].material = _swordMaterials[0];
			}
			_actionButtons[3].GetComponent<Renderer>().enabled = true;
			_actionButtons[4].GetComponent<Renderer>().enabled = true;
			_actionButtons[3].Highlight.gameObject.SetActive(true);
			_actionButtons[4].Highlight.gameObject.SetActive(true);
			_fightStarted = true;
			for (int i = 0; i <= 9; i++) {
				_fullHunger[i].enabled = false;
				_emptyHunger[i].enabled = false;
			}
			if (_playerHealth != 0) {
				_playerHealth = 0;
			}
			_mobIndicator.material = _mobMaterials[9];
			_mobIndicator.enabled = true;
			_gMonsterIndex = 10;
			_gMonsterName = _monsterIndex[10];
			_gMonsterDamage = _monsterDamage[10];
			_gMonsterHealth = _monsterHealth[10];
			_mobHealthIndicator.text = _gMonsterHealth.ToString();
			StartCoroutine(UpHearts());
			Debug.LogFormat("[Minecraft Survival #{0}]: The monster you are fighting is {1} and it has {2} health and {3} damage.", _modID, _monsterIndex[_gMonsterIndex], _monsterHealth[_gMonsterIndex], _monsterDamage[_gMonsterIndex]);
			return;
		}
		string mob = "";
		string[] mobs;
		switch (_dimensionIndex) {
			case 0:
				mobs = _monsterTable[0];
				mob = mobs[rand.Range(0, mobs.Length)];
				break;
			case 1:
				mobs = _monsterTable[1];
				mob = mobs[rand.Range(0, mobs.Length)];
				break;
			case 2:
				mobs = _monsterTable[2];
				mob = mobs[rand.Range(0, mobs.Length)];
				break;
			default:
				Debug.LogFormat("[Minecraft Survival #{0}]: Unable to create a monster fight.");
				break;
		}
		switch (mob) {
			case "Zombie":
				_mobIndicator.material = _mobMaterials[0];
				_gMonsterIndex = 0;
				break;
			case "Skeleton":
				_mobIndicator.material = _mobMaterials[1];
				_gMonsterIndex = 1;
				break;
			case "Spider":
				_mobIndicator.material = _mobMaterials[2];
				_gMonsterIndex = 2;
				break;
			case "Creeper":
				_mobIndicator.material = _mobMaterials[3];
				_gMonsterIndex = 3;
				break;
			case "Slime":
				_mobIndicator.material = _mobMaterials[4];
				_gMonsterIndex = 4;
				break;
			case "Pigman":
				_mobIndicator.material = _mobMaterials[0];
				_gMonsterIndex = 5;
				break;
			case "Blaze":
				_mobIndicator.material = _mobMaterials[5];
				_gMonsterIndex = 6;
				break;
			case "Wither Skeleton":
				_mobIndicator.material = _mobMaterials[6];
				_gMonsterIndex = 7;
				break;
			case "Enderman":
				_mobIndicator.material = _mobMaterials[7];
				_gMonsterIndex = 8;
				break;
			case "Shulker":
				_mobIndicator.material = _mobMaterials[8];
				_gMonsterIndex = 9;
				break;
			default:
				Debug.LogFormat("[Minecraft Survival #{0}]: Unable to find monster informaton.");
				break;
		}
		_mobIndicator.enabled = true;
		Debug.LogFormat("[Minecraft Survival #{0}]: The monster you are fighting is {1} and it has {2} health and {3} damage.", _modID, _monsterIndex[_gMonsterIndex], _monsterHealth[_gMonsterIndex], _monsterDamage[_gMonsterIndex]);
		
		foreach (KMSelectable km in _resourceButtons) {
			km.GetComponent<Renderer>().enabled = false;
			if (km.Highlight.gameObject.activeSelf) {
				km.Highlight.gameObject.SetActive(false);
			}
		}

		foreach (KMSelectable km in _actionButtons) {
			km.GetComponent<Renderer>().enabled = false;
			if (km.Highlight.gameObject.activeSelf) {
				km.Highlight.gameObject.SetActive(false);
			}
		}

		if (_materialValues[14] >= 1) {
			_actionButtonMeshes[3].material = _swordMaterials[3];
		} else if (_materialValues[13] >= 1) {
			_actionButtonMeshes[3].material = _swordMaterials[2];
		} else if (_materialValues[12] >= 1) {
			_actionButtonMeshes[3].material = _swordMaterials[1];
		} else if (_materialValues[11] >= 1) {
			_actionButtonMeshes[3].material = _swordMaterials[0];
		}
		_actionButtons[3].GetComponent<Renderer>().enabled = true;
		_actionButtons[4].GetComponent<Renderer>().enabled = true;
		_actionButtons[3].Highlight.gameObject.SetActive(true);
		_actionButtons[4].Highlight.gameObject.SetActive(true);
		_fightStarted = true;
		_gMonsterName = _monsterIndex[_gMonsterIndex];
		_gMonsterHealth = _monsterHealth[_gMonsterIndex];
		_gMonsterDamage = _monsterDamage[_gMonsterIndex];
		_mobHealthIndicator.text = _gMonsterHealth.ToString();
		if (_playerHealth != 0) {
			_playerHealth = 0;
		}
		for (int i = 0; i <= 9; i++) {
			_fullHunger[i].enabled = false;
			_emptyHunger[i].enabled = false;
		}
		StartCoroutine(UpHearts());
	}

	void FightEventHandler() {
		if (_playerDamage != 20) {
			if (_materialValues[14] >= 1) {
				_playerDamage = _swordDamages[3];
			} else if (_materialValues[13] >= 1) {
				_playerDamage = _swordDamages[2];
			} else if (_materialValues[12] >= 1) {
				_playerDamage = _swordDamages[1];
			} else if (_materialValues[11] >= 1) {
				_playerDamage = _swordDamages[0];
			}
		}
		if (_playerProc == 0) {
			if (_materialValues[16] >= 1) {
				_playerProc = _armorProc[1];
			} else if (_materialValues[15] >= 1) {
				_playerProc = _armorProc[0];
			} else {
				_playerProc = 0;
			}
		}
		_gMonsterHealth -= _playerDamage;
		Debug.LogFormat("[Minecraft Survival #{0}]: You attacked the {1} for {2} damage and now has {3} health remaining.", _modID, _gMonsterName, _playerDamage, _gMonsterHealth < 0 ? 0 : _gMonsterHealth);
		_mobHealthIndicator.text = _gMonsterHealth < 0 ? 0.ToString() : _gMonsterHealth.ToString();
		if (_dragonFightStarted && _gMonsterHealth <= 0) {
			Debug.LogFormat("[Minecraft Survival #{0}]: Dragon defeated. Go click on the egg!", _modID);
			_dragonDefeated = true;
			_dragonFightStarted = false;
			_mobIndicator.enabled = false;
			_mobHealthIndicator.text = "";
			UpdateModule();
			_materialValues[41]++;
			return;
		}
		if (_gMonsterHealth <= 0) {
			_fightStarted = false;
			_fightReset = true;
			_resourceUntilFight = rand.Range(6, 13);
			_mobIndicator.enabled = false;
			_mobHealthIndicator.text = "";
			UpdateModule();
			GiveMobDrop();
			Debug.LogFormat("[Minecraft Survival #{0}]: The monster has been defeated. Resume gathering! Number of resources gathered till next fight: {1}.", _modID, _resourceUntilFight);
			return;
		}
		int monsterChance = !(_gMonsterName.Equals("Ender Dragon")) ? rand.Range(1, 5) : rand.Range(1, 26);
		if (monsterChance == 1 && _gMonsterName.Equals("Ender Dragon")) {
			_playerHealth -= (_gMonsterDamage - _playerProc) < 0 ? 0 : (_gMonsterDamage - _playerProc);
			if (_playerHealth <= 0) {
				_fightStarted = false;
				_fightReset = true;
				_resourceUntilFight = rand.Range(6, 13);
				_mobIndicator.enabled = false;
				_mobHealthIndicator.text = "";
				UpdateModule();
				Debug.LogFormat("[Minecraft Survival #{0}]: You died by a {1}. Strike!", _modID, _gMonsterName);
				GetComponent<KMBombModule>().HandleStrike();
				return;
			}
			Debug.LogFormat("[Minecraft Survival #{0}]: You have been attacked by the {1} for {2} damage and now have {3} health remaining.", _modID, _gMonsterName, (_gMonsterDamage - _playerProc), _playerHealth < 0 ? 0 : _playerHealth);
			UpdateHearts();
			return;
		}
		if (monsterChance.EqualsAny(1,2) && !(_gMonsterName.Equals("Creeper"))) {
			_playerHealth -= (_gMonsterDamage-_playerProc) < 0 ? 0 : (_gMonsterDamage-_playerProc);
			if (_playerHealth <= 0) {
				_fightStarted = false;
				_fightReset = true;
				_resourceUntilFight = rand.Range(6, 13);
				_mobIndicator.enabled = false;
				_mobHealthIndicator.text = "";
				UpdateModule();
				Debug.LogFormat("[Minecraft Survival #{0}]: You died by a {1}. Strike!", _modID, _gMonsterName);
				GetComponent<KMBombModule>().HandleStrike();
				return;
			}
			Debug.LogFormat("[Minecraft Survival #{0}]: You have been attacked by the {1} for {2} damage and now have {3} health remaining.", _modID, _gMonsterName, (_gMonsterDamage-_playerProc), _playerHealth < 0 ? 0 : _playerHealth);
			UpdateHearts();
			return;
		} else if (monsterChance == 1 && _gMonsterName.Equals("Creeper")) {
			_playerHealth -= (_gMonsterDamage - _playerProc) < 0 ? 0 : (_gMonsterDamage - _playerProc);
			if (_playerHealth <= 0) {
				_fightStarted = false;
				_fightReset = true;
				_resourceUntilFight = rand.Range(6, 13);
				_mobIndicator.enabled = false;
				_mobHealthIndicator.text = "";
				UpdateModule();
				Debug.LogFormat("[Minecraft Survival #{0}]: A Creeper blew you up. You died, aw man. Strike!", _modID);
				GetComponent<KMBombModule>().HandleStrike();
				return;
			}
			Debug.LogFormat("[Minecraft Survival #{0}]: You have been attacked by the {1} for {2} damage and now have {3} health remaining.", _modID, _gMonsterName, (_gMonsterDamage - _playerProc), _playerHealth);
			UpdateHearts();
			return;
		}
	}

	void EatEventHandler() {
		if (_fightStarted || _dragonFightStarted) {
			if (_playerHealth == 20) { return; }
			if (_materialValues[4] >= 1) {
				_materialValues[4]--;
				_playerHealth = (_playerHealth + 7) > 20 ? 20 : (_playerHealth + 7);
				UpdateHearts();
			}
		} else {
			if (_playerHunger == 10) { return; }
			if (_materialValues[4] >= 1) {
				_materialValues[4]--;
				_playerHunger = (_playerHunger + 4) > 10 ? 10 : (_playerHunger + 4);
				UpdateHunger();
			}
		}
	}

	void StartTheDamnDragonFightAlready() {
		_dragonFightStarted = true;
		StartFightEventHandler();
	}

	void UpdateHearts() {
		switch (_playerHealth) {
			case 0:
				foreach (SpriteRenderer sr in _fullHearts) {
					sr.enabled = false;
				}
				foreach (SpriteRenderer sr in _halfHearts) {
					sr.enabled = false;
				}
				foreach (SpriteRenderer sr in _emptyHearts) {
					sr.enabled = true;
				}
				break;
			case 1:
				for (int i = 0; i <= 9; i++) {
					_fullHearts[i].enabled = false;
					if (i == 0) { _halfHearts[i].enabled = true; } else { _halfHearts[i].enabled = false; }
					if (i == 0) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 2:
				for (int i = 0; i <= 9; i++)
				{
					if (i == 0) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false;  }
					_halfHearts[i].enabled = false;
					if (i == 0) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 3:
				for (int i = 0; i <= 9; i++)
				{
					if (i == 0) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					if (i == 1) { _halfHearts[i].enabled = true; } else { _halfHearts[i].enabled = false; };
					if (i.EqualsAny(0,1)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 4:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					_halfHearts[i].enabled = false;
					if (i.EqualsAny(0, 1)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 5:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					if (i == 2) { _halfHearts[i].enabled = true; } else { _halfHearts[i].enabled = false; };
					if (i.EqualsAny(0, 1, 2)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 6:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					_halfHearts[i].enabled = false;
					if (i.EqualsAny(0, 1, 2)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 7:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					if (i == 3) { _halfHearts[i].enabled = true; } else { _halfHearts[i].enabled = false; };
					if (i.EqualsAny(0, 1, 2, 3)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 8:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					_halfHearts[i].enabled = false;
					if (i.EqualsAny(0, 1, 2, 3)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 9:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					if (i == 4) { _halfHearts[i].enabled = true; } else { _halfHearts[i].enabled = false; };
					if (i.EqualsAny(0, 1, 2, 3, 4)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 10:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					_halfHearts[i].enabled = false;
					if (i.EqualsAny(0, 1, 2, 3, 4)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 11:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					if (i == 5) { _halfHearts[i].enabled = true; } else { _halfHearts[i].enabled = false; };
					if (i.EqualsAny(0, 1, 2, 3, 4, 5)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 12:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4, 5)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					_halfHearts[i].enabled = false;
					if (i.EqualsAny(0, 1, 2, 3, 4, 5)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 13:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4, 5)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					if (i == 6) { _halfHearts[i].enabled = true; } else { _halfHearts[i].enabled = false; };
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 14:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					_halfHearts[i].enabled = false;
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 15:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					if (i == 7) { _halfHearts[i].enabled = true; } else { _halfHearts[i].enabled = false; };
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 16:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					_halfHearts[i].enabled = false;
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 17:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					if (i == 8) { _halfHearts[i].enabled = true; } else { _halfHearts[i].enabled = false; };
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7, 8)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 18:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7, 8)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					_halfHearts[i].enabled = false;
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7, 8)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 19:
				for (int i = 0; i <= 9; i++)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7, 8)) { _fullHearts[i].enabled = true; } else { _fullHearts[i].enabled = false; }
					if (i == 9) { _halfHearts[i].enabled = true; } else { _halfHearts[i].enabled = false; };
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7, 8, 9)) { _emptyHearts[i].enabled = false; } else { _emptyHearts[i].enabled = true; }
				}
				break;
			case 20:
				foreach (SpriteRenderer sr in _fullHearts)
				{
					sr.enabled = true;
				}
				foreach (SpriteRenderer sr in _halfHearts)
				{
					sr.enabled = false;
				}
				foreach (SpriteRenderer sr in _emptyHearts)
				{
					sr.enabled = false;
				}
				break;
			default:
				Debug.LogFormat("[Minecraft Survival #{0}]: Unable to update player health indicator.", _modID);
				break;
		}
	}

	void UpdateHunger() {
		if (_fightReset) {
			_playerHunger = 10;
			_fightReset = false;
		}
		switch (_playerHunger) {
			case 0:
				foreach (SpriteRenderer sr in _fullHunger) {
					sr.enabled = false;
				}
				foreach (SpriteRenderer sr in _emptyHunger) {
					sr.enabled = true;
				}
				break;
			case 1:
				for (int i = 0; i <= 9; i++) {
					if (i == 0) { _fullHunger[i].enabled = true; _emptyHunger[i].enabled = false; continue; }
					_fullHunger[i].enabled = false; 
					_emptyHunger[i].enabled = true;
				}
				break;
			case 2:
				for (int i = 0; i <= 9; i++) {
					if (i.EqualsAny(0, 1)) { _fullHunger[i].enabled = true; _emptyHunger[i].enabled = false; continue; }
					_fullHunger[i].enabled = false; 
					_emptyHunger[i].enabled = true;
				}
				break;
			case 3:
				for (int i = 0; i <= 9; i++) {
					if (i.EqualsAny(0, 1, 2)) { _fullHunger[i].enabled = true; _emptyHunger[i].enabled = false; continue; }
					_fullHunger[i].enabled = false; 
					_emptyHunger[i].enabled = true;
				}
				break;
			case 4:
				for (int i = 0; i <= 9; i++) {
					if (i.EqualsAny(0, 1, 2, 3)) { _fullHunger[i].enabled = true; _emptyHunger[i].enabled = false; continue; }
					_fullHunger[i].enabled = false; 
					_emptyHunger[i].enabled = true;
				}
				break;
			case 5:
				for (int i = 0; i <= 9; i++) {
					if (i.EqualsAny(0, 1, 2, 3, 4)) { _fullHunger[i].enabled = true; _emptyHunger[i].enabled = false; continue; }
					_fullHunger[i].enabled = false; 
					_emptyHunger[i].enabled = true;
				}
				break;
			case 6:
				for (int i = 0; i <= 9; i++) {
					if (i.EqualsAny(0, 1, 2, 3, 4, 5)) { _fullHunger[i].enabled = true; _emptyHunger[i].enabled = false; continue; }
					_fullHunger[i].enabled = false; 
					_emptyHunger[i].enabled = true;
				}
				break;
			case 7:
				for (int i = 0; i <= 9; i++) {
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6)) { _fullHunger[i].enabled = true; _emptyHunger[i].enabled = false; continue; }
					_fullHunger[i].enabled = false; 
					_emptyHunger[i].enabled = true;
				}
				break;
			case 8:
				for (int i = 0; i <= 9; i++) {
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7)) { _fullHunger[i].enabled = true; _emptyHunger[i].enabled = false; continue; }
					_fullHunger[i].enabled = false; 
					_emptyHunger[i].enabled = true;
				}
				break;
			case 9:
				for (int i = 0; i <= 9; i++) {
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7, 8)) { _fullHunger[i].enabled = true; _emptyHunger[i].enabled = false; continue; }
					_fullHunger[i].enabled = false; 
					_emptyHunger[i].enabled = true;
				}
				break;
			case 10:
				foreach (SpriteRenderer sr in _fullHunger) {
					sr.enabled = true;
				}
				foreach (SpriteRenderer sr in _emptyHunger) {
					sr.enabled = false;
				}
				break;
			default:
				Debug.LogFormat("[Minecraft Survival #{0}]: Unable to update hunger bar.",_modID);
				break;
		}
	}

	void UpdateModule() {
		_actionButtons[3].GetComponent<Renderer>().enabled = false;
		_actionButtons[4].GetComponent<Renderer>().enabled = false;
		_actionButtons[3].Highlight.gameObject.SetActive(false);
		_actionButtons[4].Highlight.gameObject.SetActive(false);
		int i = 0;
		foreach (KMSelectable action in _actionButtons)
		{
			if (i.EqualsAny(3, 4, 7)) { i++; continue; }
			action.GetComponent<Renderer>().enabled = true; 
			action.Highlight.gameObject.SetActive(true);
			i++;
		}
		i = 0;
		switch (_dimensionIndex) {
			case 0:
				foreach (KMSelectable resource in _resourceButtons)
				{
					if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7)) { resource.GetComponent<Renderer>().enabled = true; resource.Highlight.gameObject.SetActive(true); i++; continue; }
					resource.GetComponent<Renderer>().enabled = false; resource.Highlight.gameObject.SetActive(false);
					i++;
				}
				if (!_dragonDefeated) {
					_actionButtons[7].GetComponent<Renderer>().enabled = false; _actionButtons[7].Highlight.gameObject.SetActive(false);
				}
				_dimensionIndex = 0;
				_moduleMeshes[0].material = _dimensionMaterials[0];
				_moduleMeshes[1].material = _dimensionMaterials[1];
				break;
			case 1:
				foreach (KMSelectable resource in _resourceButtons)
				{
					if (i.EqualsAny(8, 9, 10, 11)) { resource.GetComponent<Renderer>().enabled = true; resource.Highlight.gameObject.SetActive(true); i++; continue; }
					resource.GetComponent<Renderer>().enabled = false; resource.Highlight.gameObject.SetActive(false);
					i++;
				}
				if (!_dragonDefeated) {
					_actionButtons[7].GetComponent<Renderer>().enabled = false; _actionButtons[7].Highlight.gameObject.SetActive(false);
				}
				_dimensionIndex = 1;
				_moduleMeshes[0].material = _dimensionMaterials[2];
				_moduleMeshes[1].material = _dimensionMaterials[3];
				break;
			case 2:
				foreach (KMSelectable resource in _resourceButtons)
				{
					if (i.EqualsAny(12,13,14,15)) { resource.GetComponent<Renderer>().enabled = true; resource.Highlight.gameObject.SetActive(true); i++; continue; }
					resource.GetComponent<Renderer>().enabled = false; resource.Highlight.gameObject.SetActive(false);
					i++;
				}
				if (!_dragonDefeated) {
					_actionButtons[7].GetComponent<Renderer>().enabled = true; _actionButtons[7].Highlight.gameObject.SetActive(true);
				}
				_dimensionIndex = 2;
				_moduleMeshes[0].material = _dimensionMaterials[4];
				_moduleMeshes[1].material = _dimensionMaterials[5];
				break;
			default:
				Debug.LogFormat("[Minecraft Survival #{0}]: Unable to update module.",_modID);
				break;
		}
		for (int g = 0; g <= 9; g++) {
			_emptyHearts[g].enabled = false;
			_halfHearts[g].enabled = false;
			_fullHearts[g].enabled = false;
		}
		UpdateHunger();
	}

	void GiveMobDrop() {
		switch (_gMonsterName)
		{
			case "Zombie":
				_materialValues[35]++;
				break;
			case "Skeleton":
				_materialValues[36] += rand.Range(2,4);
				break;
			case "Spider":
				_materialValues[37] += 2;
				break;
			case "Creeper":
				_materialValues[38]++;
				break;
			case "Slime":
				_materialValues[39] += rand.Range(2,4);
				break;
			case "Pigman":
				_materialValues[35] += 2;
				break;
			case "Blaze":
				_materialValues[32] += 2;
				break;
			case "Wither Skeleton":
				_materialValues[20] += rand.Range(2,5);
				break;
			case "Enderman":
				_materialValues[33]++;
				break;
			case "Shulker":
				_materialValues[40]++;
				break;
			default:
				Debug.LogFormat("[Minecraft Survival #{0}]: Unable to find monster informaton.");
				break;
		}
	}

	void SolveModule() {
		foreach (KMSelectable sr in _actionButtons) 
		{
			sr.GetComponent<Renderer>().enabled = false;
			sr.Highlight.gameObject.SetActive(false);
		}
		foreach (KMSelectable sr in _resourceButtons)
		{
			sr.GetComponent<Renderer>().enabled = false;
			sr.Highlight.gameObject.SetActive(false);
		}
		foreach (KMSelectable sr in _allResourceInventoryIndicies)
		{
			sr.GetComponent<Renderer>().enabled = false;
			sr.Highlight.gameObject.SetActive(false);
		}
		for (int i = 0; i <= 9; i++) {
			_emptyHunger[i].enabled = false;
			_fullHunger[i].enabled = false;
			_emptyHearts[i].enabled = false;
			_halfHearts[i].enabled = false;
			_fullHearts[i].enabled = false;
		}
		_materialValueText.text = "";
		_mobHealthIndicator.text = "";
		_moduleMeshes[0].material = _dimensionMaterials[8];
		_moduleMeshes[1].material = _dimensionMaterials[8];
		_modSolved = true;
	}

	IEnumerator OpenInventory() {
		_isAnimating = true;
		_moduleMeshes[0].material = _dimensionMaterials[6];
		_moduleMeshes[1].material = _dimensionMaterials[7];
		for (int i = 0; i <= 9; i++) {
			_fullHunger[i].enabled = false;
			_emptyHunger[i].enabled = false;
		}
		foreach (KMSelectable action in _actionButtons) {
			action.GetComponent<Renderer>().enabled = false;
			action.Highlight.gameObject.SetActive(false);
		}
		foreach (KMSelectable resource in _resourceButtons)
		{
			resource.GetComponent<Renderer>().enabled = false;
			resource.Highlight.gameObject.SetActive(false);
		}
		foreach (KMSelectable inv in _allResourceInventoryIndicies) {
			inv.GetComponent<Renderer>().enabled = true;
			yield return new WaitForSeconds(0.03f);
		}
		foreach (KMSelectable inv in _allResourceInventoryIndicies) {
			inv.Highlight.gameObject.SetActive(true);
		}
		_isAnimating = false;
		_materialValueText.text = "-";
		_actionButtons[5].GetComponent<Renderer>().enabled = true;
		_actionButtons[5].Highlight.gameObject.SetActive(true);
		yield break;
	}

	IEnumerator CloseInventory() {
		KMSelectable[] kma = new KMSelectable[44];
		_allResourceInventoryIndicies.CopyTo(kma,0);
		_materialValueText.text = "";
		_isAnimating = true;
		_actionButtons[5].GetComponent<Renderer>().enabled = false;
		_actionButtons[5].Highlight.gameObject.SetActive(false);
		foreach (KMSelectable inv in kma.Reverse())
		{
			inv.Highlight.gameObject.SetActive(false);
		}
		foreach (KMSelectable inv in kma.Reverse())
		{
			inv.GetComponent<Renderer>().enabled = false;
			yield return new WaitForSeconds(0.03f);
		}
		UpdateModule();
		UpdateHunger();
		_isAnimating = false;
		yield break;
	}

	IEnumerator UpdateHighlights() {
		yield return new WaitForSeconds(0.7f);
		int i = 0;
		foreach (KMSelectable action in _actionButtons)
		{
			if (i.EqualsAny(0, 1, 2, 5, 6)) { i++; continue; }
			action.Highlight.gameObject.SetActive(false);
			i++;
		}
		i = 0;
		foreach (KMSelectable resource in _resourceButtons)
		{
			if (i.EqualsAny(0, 1, 2, 3, 4, 5, 6, 7)) { i++; continue; }
			resource.Highlight.gameObject.SetActive(false);
			i++;
		}
		foreach (KMSelectable inv in _allResourceInventoryIndicies)
		{
			inv.Highlight.gameObject.SetActive(false);
		}
		_materialValues[9] = 1;
		_materialValues[4] = 10;
		yield break;
	}

	IEnumerator UpHearts() {
		_isAnimating = true;
		while (_playerHealth <= 20) {
			foreach (SpriteRenderer sr in _regenHearts) {
				sr.enabled = true;
			}
			yield return new WaitForSeconds(0.02f);
			foreach (SpriteRenderer sr in _regenHearts)
			{
				sr.enabled = false;
			}
			UpdateHearts();
			yield return new WaitForSeconds(0.02f);
			_playerHealth++;
		}
		_isAnimating = false;
		_playerHealth = 20;
		yield break;
	}

	IEnumerator UpHunger() {
		_isAnimating = true;
		while (_playerHunger <= 10) {
			UpdateHunger();
			_playerHunger++;
			yield return new WaitForSeconds(0.1f);
		}
		_isAnimating = false;
		_playerHunger = 10;
		yield break;
	}
}
