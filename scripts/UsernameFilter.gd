class_name UsernameFilter
## TEMPORARY client-side slur filter for usernames. Slurs only - ordinary
## profanity (fuck, shit, ...) is intentionally allowed. This is a stopgap that a
## modified client trivially bypasses; the real check must be server-side on
## /create-user. When that lands, delete this file and the call in Registration.gd.
##
## Terms are base64 so the shipped client isn't a plaintext slur dictionary.
## _SUB = matched anywhere in the (normalised) name. _EX = must BE the whole name
## (short / ambiguous roots like "spade", "mick" that legitimately appear inside
## longer names).

const _SUB: PackedStringArray = [
	"bmlnZ2Vy", "bmlnZ2E=", "ZmFnZ290", "Y2hpbms=", "d2V0YmFjaw==", "YmVhbmVy",
	"c2FuZG5pZ2dlcg==", "amlnYWJvbw==", "cG9yY2htb25rZXk=", "emlwcGVyaGVhZA==", "dG93ZWxoZWFk", "cmFnaGVhZA==",
	"dHJhbm55", "cmV0YXJk", "a2FmZmly", "cmVkc2tpbg==", "c2hlbWFsZQ==", "ZGFya2ll",
	"a2lrZQ==",
]

const _EX: PackedStringArray = [
	"c3BpYw==", "Y29vbg==", "ZmFn", "ZHlrZQ==", "cGFraQ==", "bmVncm8=",
	"Z3lw", "Z3lwbw==", "Z3lwcG8=", "d29w", "ZGFnbw==", "a3JhdXQ=",
	"aW5qdW4=", "c3F1YXc=", "YWJibw==", "aGVlYg==", "a2FmaXI=", "c3BhZGU=",
	"c3Bvb2s=", "bWljaw==", "cG9sYWNr", "dGFyZA==", "amFw", "Y2hpbmFtYW4=",
	"Z29vaw==", "a2tr",
]

## digit / punctuation / common Cyrillic + Greek look-alikes -> ascii
const _MAP := {
	"0": "o", "1": "i", "3": "e", "4": "a", "5": "s", "7": "t", "8": "b", "9": "g",
	"@": "a", "$": "s", "!": "i", "|": "i", "+": "t",
	"а": "a", "е": "e", "о": "o", "р": "p", "с": "c", "у": "y", "х": "x", "к": "k",
	"м": "m", "н": "h", "т": "t", "в": "b", "і": "i", "ѕ": "s", "ԁ": "d", "ɡ": "g",
	"α": "a", "ο": "o", "ρ": "p", "ν": "v", "ι": "i", "κ": "k",
}

static var _loaded := false
static var _substr: PackedStringArray = []
static var _exact: PackedStringArray = []


static func _decode(src: PackedStringArray, dst: PackedStringArray) -> void:
	for e in src:
		var w := _normalize(Marshalls.base64_to_utf8(e))
		if w != "":
			dst.append(w)


static func _ensure() -> void:
	if _loaded:
		return
	_loaded = true
	_decode(_SUB, _substr)
	_decode(_EX, _exact)


## Fold leetspeak / look-alikes to ascii, drop everything but a-z, and squash any
## run of the same letter down to 2 so "niiiigger"-style spam still lands.
static func _normalize(s: String) -> String:
	var res := ""
	var last := ""
	var run := 0
	for ch in s.to_lower():
		var m: String = _MAP.get(ch, ch)
		if m.length() != 1 or m < "a" or m > "z":
			continue
		if m == last:
			run += 1
			if run >= 2:
				continue
		else:
			run = 0
		res += m
		last = m
	return res


## True if the name contains (or, for the ambiguous roots, exactly is) a slur.
static func contains_slur(name: String) -> bool:
	_ensure()
	var n := _normalize(name)
	if n == "":
		return false
	for e in _exact:
		if n == e:
			return true
	for sub in _substr:
		if n.contains(sub):
			return true
	return false
