using Microsoft.Xna.Framework;

namespace GameProject {
    /// <summary>
    /// Preview the colors: https://tailwindcss.com/docs/customizing-colors#color-palette-reference
    /// </summary>
    public class TWColor {
        /// <summary>Black color (R:0,G:0,B:0,A:255).</summary>
        public static readonly Color Black = new Color(0, 0, 0);
        /// <summary>White color (R:255,G:255,B:255,A:255).</summary>
        public static readonly Color White = new Color(255, 255, 255);
        /// <summary>White color (R:0,G:0,B:0,A:0).</summary>
        public static readonly Color Transparent = new Color(0, 0, 0, 0);

        /// <summary>BlueGray050 color (R:248,G:250,B:252,A:255).</summary>
        public static readonly Color BlueGray050 = new Color(248, 250, 252);
        /// <summary>BlueGray100 color (R:241,G:245,B:249,A:255).</summary>
        public static readonly Color BlueGray100 = new Color(241, 245, 249);
        /// <summary>BlueGray200 color (R:226,G:232,B:240,A:255).</summary>
        public static readonly Color BlueGray200 = new Color(226, 232, 240);
        /// <summary>BlueGray300 color (R:203,G:213,B:225,A:255).</summary>
        public static readonly Color BlueGray300 = new Color(203, 213, 225);
        /// <summary>BlueGray400 color (R:148,G:163,B:184,A:255).</summary>
        public static readonly Color BlueGray400 = new Color(148, 163, 184);
        /// <summary>BlueGray500 color (R:100,G:116,B:139,A:255).</summary>
        public static readonly Color BlueGray500 = new Color(100, 116, 139);
        /// <summary>BlueGray600 color (R:71,G:85,B:105,A:255).</summary>
        public static readonly Color BlueGray600 = new Color(71, 85, 105);
        /// <summary>BlueGray700 color (R:51,G:65,B:85,A:255).</summary>
        public static readonly Color BlueGray700 = new Color(51, 65, 85);
        /// <summary>BlueGray800 color (R:30,G:41,B:59,A:255).</summary>
        public static readonly Color BlueGray800 = new Color(30, 41, 59);
        /// <summary>BlueGray900 color (R:15,G:23,B:42,A:255).</summary>
        public static readonly Color BlueGray900 = new Color(15, 23, 42);

        /// <summary>CoolGray050 color (R:249,G:250,B:251,A:255).</summary>
        public static readonly Color CoolGray050 = new Color(249, 250, 251);
        /// <summary>CoolGray100 color (R:243,G:244,B:246,A:255).</summary>
        public static readonly Color CoolGray100 = new Color(243, 244, 246);
        /// <summary>CoolGray200 color (R:229,G:231,B:235,A:255).</summary>
        public static readonly Color CoolGray200 = new Color(229, 231, 235);
        /// <summary>CoolGray300 color (R:209,G:213,B:219,A:255).</summary>
        public static readonly Color CoolGray300 = new Color(209, 213, 219);
        /// <summary>CoolGray400 color (R:156,G:163,B:175,A:255).</summary>
        public static readonly Color CoolGray400 = new Color(156, 163, 175);
        /// <summary>CoolGray500 color (R:107,G:114,B:128,A:255).</summary>
        public static readonly Color CoolGray500 = new Color(107, 114, 128);
        /// <summary>CoolGray600 color (R:75,G:85,B:99,A:255).</summary>
        public static readonly Color CoolGray600 = new Color(75, 85, 99);
        /// <summary>CoolGray700 color (R:55,G:65,B:81,A:255).</summary>
        public static readonly Color CoolGray700 = new Color(55, 65, 81);
        /// <summary>CoolGray800 color (R:31,G:41,B:55,A:255).</summary>
        public static readonly Color CoolGray800 = new Color(31, 41, 55);
        /// <summary>CoolGray900 color (R:17,G:24,B:39,A:255).</summary>
        public static readonly Color CoolGray900 = new Color(17, 24, 39);

        /// <summary>Gray050 color (R:250,G:250,B:250,A:255).</summary>
        public static readonly Color Gray050 = new Color(250, 250, 250);
        /// <summary>Gray100 color (R:244,G:244,B:245,A:255).</summary>
        public static readonly Color Gray100 = new Color(244, 244, 245);
        /// <summary>Gray200 color (R:228,G:228,B:231,A:255).</summary>
        public static readonly Color Gray200 = new Color(228, 228, 231);
        /// <summary>Gray300 color (R:212,G:212,B:216,A:255).</summary>
        public static readonly Color Gray300 = new Color(212, 212, 216);
        /// <summary>Gray400 color (R:161,G:161,B:170,A:255).</summary>
        public static readonly Color Gray400 = new Color(161, 161, 170);
        /// <summary>Gray500 color (R:113,G:113,B:122,A:255).</summary>
        public static readonly Color Gray500 = new Color(113, 113, 122);
        /// <summary>Gray600 color (R:82,G:82,B:91,A:255).</summary>
        public static readonly Color Gray600 = new Color(82, 82, 91);
        /// <summary>Gray700 color (R:63,G:63,B:70,A:255).</summary>
        public static readonly Color Gray700 = new Color(63, 63, 70);
        /// <summary>Gray800 color (R:39,G:39,B:42,A:255).</summary>
        public static readonly Color Gray800 = new Color(39, 39, 42);
        /// <summary>Gray900 color (R:24,G:24,B:27,A:255).</summary>
        public static readonly Color Gray900 = new Color(24, 24, 27);

        /// <summary>TrueGray050 color (R:250,G:250,B:250,A:255).</summary>
        public static readonly Color TrueGray050 = new Color(250, 250, 250);
        /// <summary>TrueGray100 color (R:245,G:245,B:245,A:255).</summary>
        public static readonly Color TrueGray100 = new Color(245, 245, 245);
        /// <summary>TrueGray200 color (R:229,G:229,B:229,A:255).</summary>
        public static readonly Color TrueGray200 = new Color(229, 229, 229);
        /// <summary>TrueGray300 color (R:212,G:212,B:212,A:255).</summary>
        public static readonly Color TrueGray300 = new Color(212, 212, 212);
        /// <summary>TrueGray400 color (R:163,G:163,B:163,A:255).</summary>
        public static readonly Color TrueGray400 = new Color(163, 163, 163);
        /// <summary>TrueGray500 color (R:115,G:115,B:115,A:255).</summary>
        public static readonly Color TrueGray500 = new Color(115, 115, 115);
        /// <summary>TrueGray600 color (R:82,G:82,B:82,A:255).</summary>
        public static readonly Color TrueGray600 = new Color(82, 82, 82);
        /// <summary>TrueGray700 color (R:64,G:64,B:64,A:255).</summary>
        public static readonly Color TrueGray700 = new Color(64, 64, 64);
        /// <summary>TrueGray800 color (R:38,G:38,B:38,A:255).</summary>
        public static readonly Color TrueGray800 = new Color(38, 38, 38);
        /// <summary>TrueGray900 color (R:23,G:23,B:23,A:255).</summary>
        public static readonly Color TrueGray900 = new Color(23, 23, 23);

        /// <summary>WarmGray050 color (R:250,G:250,B:249,A:255).</summary>
        public static readonly Color WarmGray050 = new Color(250, 250, 249);
        /// <summary>WarmGray100 color (R:245,G:245,B:244,A:255).</summary>
        public static readonly Color WarmGray100 = new Color(245, 245, 244);
        /// <summary>WarmGray200 color (R:231,G:229,B:228,A:255).</summary>
        public static readonly Color WarmGray200 = new Color(231, 229, 228);
        /// <summary>WarmGray300 color (R:214,G:211,B:209,A:255).</summary>
        public static readonly Color WarmGray300 = new Color(214, 211, 209);
        /// <summary>WarmGray400 color (R:168,G:162,B:158,A:255).</summary>
        public static readonly Color WarmGray400 = new Color(168, 162, 158);
        /// <summary>WarmGray500 color (R:120,G:113,B:108,A:255).</summary>
        public static readonly Color WarmGray500 = new Color(120, 113, 108);
        /// <summary>WarmGray600 color (R:87,G:83,B:78,A:255).</summary>
        public static readonly Color WarmGray600 = new Color(87, 83, 78);
        /// <summary>WarmGray700 color (R:68,G:64,B:60,A:255).</summary>
        public static readonly Color WarmGray700 = new Color(68, 64, 60);
        /// <summary>WarmGray800 color (R:41,G:37,B:36,A:255).</summary>
        public static readonly Color WarmGray800 = new Color(41, 37, 36);
        /// <summary>WarmGray900 color (R:28,G:25,B:23,A:255).</summary>
        public static readonly Color WarmGray900 = new Color(28, 25, 23);

        /// <summary>Red050 color (R:254,G:242,B:242,A:255).</summary>
        public static readonly Color Red050 = new Color(254, 242, 242);
        /// <summary>Red100 color (R:254,G:226,B:226,A:255).</summary>
        public static readonly Color Red100 = new Color(254, 226, 226);
        /// <summary>Red200 color (R:254,G:202,B:202,A:255).</summary>
        public static readonly Color Red200 = new Color(254, 202, 202);
        /// <summary>Red300 color (R:252,G:165,B:165,A:255).</summary>
        public static readonly Color Red300 = new Color(252, 165, 165);
        /// <summary>Red400 color (R:248,G:113,B:113,A:255).</summary>
        public static readonly Color Red400 = new Color(248, 113, 113);
        /// <summary>Red500 color (R:239,G:68,B:68,A:255).</summary>
        public static readonly Color Red500 = new Color(239, 68, 68);
        /// <summary>Red600 color (R:220,G:38,B:38,A:255).</summary>
        public static readonly Color Red600 = new Color(220, 38, 38);
        /// <summary>Red700 color (R:185,G:28,B:28,A:255).</summary>
        public static readonly Color Red700 = new Color(185, 28, 28);
        /// <summary>Red800 color (R:153,G:27,B:27,A:255).</summary>
        public static readonly Color Red800 = new Color(153, 27, 27);
        /// <summary>Red900 color (R:127,G:29,B:29,A:255).</summary>
        public static readonly Color Red900 = new Color(127, 29, 29);

        /// <summary>Orange050 color (R:255,G:247,B:237,A:255).</summary>
        public static readonly Color Orange050 = new Color(255, 247, 237);
        /// <summary>Orange100 color (R:255,G:237,B:213,A:255).</summary>
        public static readonly Color Orange100 = new Color(255, 237, 213);
        /// <summary>Orange200 color (R:254,G:215,B:170,A:255).</summary>
        public static readonly Color Orange200 = new Color(254, 215, 170);
        /// <summary>Orange300 color (R:253,G:186,B:116,A:255).</summary>
        public static readonly Color Orange300 = new Color(253, 186, 116);
        /// <summary>Orange400 color (R:251,G:146,B:60,A:255).</summary>
        public static readonly Color Orange400 = new Color(251, 146, 60);
        /// <summary>Orange500 color (R:249,G:115,B:22,A:255).</summary>
        public static readonly Color Orange500 = new Color(249, 115, 22);
        /// <summary>Orange600 color (R:234,G:88,B:12,A:255).</summary>
        public static readonly Color Orange600 = new Color(234, 88, 12);
        /// <summary>Orange700 color (R:194,G:65,B:12,A:255).</summary>
        public static readonly Color Orange700 = new Color(194, 65, 12);
        /// <summary>Orange800 color (R:154,G:52,B:18,A:255).</summary>
        public static readonly Color Orange800 = new Color(154, 52, 18);
        /// <summary>Orange900 color (R:124,G:45,B:18,A:255).</summary>
        public static readonly Color Orange900 = new Color(124, 45, 18);

        /// <summary>Amber050 color (R:255,G:251,B:235,A:255).</summary>
        public static readonly Color Amber050 = new Color(255, 251, 235);
        /// <summary>Amber100 color (R:254,G:243,B:199,A:255).</summary>
        public static readonly Color Amber100 = new Color(254, 243, 199);
        /// <summary>Amber200 color (R:253,G:230,B:138,A:255).</summary>
        public static readonly Color Amber200 = new Color(253, 230, 138);
        /// <summary>Amber300 color (R:252,G:211,B:77,A:255).</summary>
        public static readonly Color Amber300 = new Color(252, 211, 77);
        /// <summary>Amber400 color (R:251,G:191,B:36,A:255).</summary>
        public static readonly Color Amber400 = new Color(251, 191, 36);
        /// <summary>Amber500 color (R:245,G:158,B:11,A:255).</summary>
        public static readonly Color Amber500 = new Color(245, 158, 11);
        /// <summary>Amber600 color (R:217,G:119,B:6,A:255).</summary>
        public static readonly Color Amber600 = new Color(217, 119, 6);
        /// <summary>Amber700 color (R:180,G:83,B:9,A:255).</summary>
        public static readonly Color Amber700 = new Color(180, 83, 9);
        /// <summary>Amber800 color (R:146,G:64,B:14,A:255).</summary>
        public static readonly Color Amber800 = new Color(146, 64, 14);
        /// <summary>Amber900 color (R:120,G:53,B:15,A:255).</summary>
        public static readonly Color Amber900 = new Color(120, 53, 15);

        /// <summary>Yellow050 color (R:255,G:251,B:235,A:255).</summary>
        public static readonly Color Yellow050 = new Color(255, 251, 235);
        /// <summary>Yellow100 color (R:254,G:243,B:199,A:255).</summary>
        public static readonly Color Yellow100 = new Color(254, 243, 199);
        /// <summary>Yellow200 color (R:253,G:230,B:138,A:255).</summary>
        public static readonly Color Yellow200 = new Color(253, 230, 138);
        /// <summary>Yellow300 color (R:252,G:211,B:77,A:255).</summary>
        public static readonly Color Yellow300 = new Color(252, 211, 77);
        /// <summary>Yellow400 color (R:251,G:191,B:36,A:255).</summary>
        public static readonly Color Yellow400 = new Color(251, 191, 36);
        /// <summary>Yellow500 color (R:245,G:158,B:11,A:255).</summary>
        public static readonly Color Yellow500 = new Color(245, 158, 11);
        /// <summary>Yellow600 color (R:217,G:119,B:6,A:255).</summary>
        public static readonly Color Yellow600 = new Color(217, 119, 6);
        /// <summary>Yellow700 color (R:180,G:83,B:9,A:255).</summary>
        public static readonly Color Yellow700 = new Color(180, 83, 9);
        /// <summary>Yellow800 color (R:146,G:64,B:14,A:255).</summary>
        public static readonly Color Yellow800 = new Color(146, 64, 14);
        /// <summary>Yellow900 color (R:120,G:53,B:15,A:255).</summary>
        public static readonly Color Yellow900 = new Color(120, 53, 15);

        /// <summary>Lime050 color (R:247,G:254,B:231,A:255).</summary>
        public static readonly Color Lime050 = new Color(247, 254, 231);
        /// <summary>Lime100 color (R:236,G:252,B:203,A:255).</summary>
        public static readonly Color Lime100 = new Color(236, 252, 203);
        /// <summary>Lime200 color (R:217,G:249,B:157,A:255).</summary>
        public static readonly Color Lime200 = new Color(217, 249, 157);
        /// <summary>Lime300 color (R:190,G:242,B:100,A:255).</summary>
        public static readonly Color Lime300 = new Color(190, 242, 100);
        /// <summary>Lime400 color (R:163,G:230,B:53,A:255).</summary>
        public static readonly Color Lime400 = new Color(163, 230, 53);
        /// <summary>Lime500 color (R:132,G:204,B:22,A:255).</summary>
        public static readonly Color Lime500 = new Color(132, 204, 22);
        /// <summary>Lime600 color (R:101,G:163,B:13,A:255).</summary>
        public static readonly Color Lime600 = new Color(101, 163, 13);
        /// <summary>Lime700 color (R:77,G:124,B:15,A:255).</summary>
        public static readonly Color Lime700 = new Color(77, 124, 15);
        /// <summary>Lime800 color (R:63,G:98,B:18,A:255).</summary>
        public static readonly Color Lime800 = new Color(63, 98, 18);
        /// <summary>Lime900 color (R:54,G:83,B:20,A:255).</summary>
        public static readonly Color Lime900 = new Color(54, 83, 20);

        /// <summary>Green050 color (R:236,G:253,B:245,A:255).</summary>
        public static readonly Color Green050 = new Color(236, 253, 245);
        /// <summary>Green100 color (R:209,G:250,B:229,A:255).</summary>
        public static readonly Color Green100 = new Color(209, 250, 229);
        /// <summary>Green200 color (R:167,G:243,B:208,A:255).</summary>
        public static readonly Color Green200 = new Color(167, 243, 208);
        /// <summary>Green300 color (R:110,G:231,B:183,A:255).</summary>
        public static readonly Color Green300 = new Color(110, 231, 183);
        /// <summary>Green400 color (R:52,G:211,B:153,A:255).</summary>
        public static readonly Color Green400 = new Color(52, 211, 153);
        /// <summary>Green500 color (R:16,G:185,B:129,A:255).</summary>
        public static readonly Color Green500 = new Color(16, 185, 129);
        /// <summary>Green600 color (R:5,G:150,B:105,A:255).</summary>
        public static readonly Color Green600 = new Color(5, 150, 105);
        /// <summary>Green700 color (R:4,G:120,B:87,A:255).</summary>
        public static readonly Color Green700 = new Color(4, 120, 87);
        /// <summary>Green800 color (R:6,G:95,B:70,A:255).</summary>
        public static readonly Color Green800 = new Color(6, 95, 70);
        /// <summary>Green900 color (R:6,G:78,B:59,A:255).</summary>
        public static readonly Color Green900 = new Color(6, 78, 59);

        /// <summary>Emerald050 color (R:236,G:253,B:245,A:255).</summary>
        public static readonly Color Emerald050 = new Color(236, 253, 245);
        /// <summary>Emerald100 color (R:209,G:250,B:229,A:255).</summary>
        public static readonly Color Emerald100 = new Color(209, 250, 229);
        /// <summary>Emerald200 color (R:167,G:243,B:208,A:255).</summary>
        public static readonly Color Emerald200 = new Color(167, 243, 208);
        /// <summary>Emerald300 color (R:110,G:231,B:183,A:255).</summary>
        public static readonly Color Emerald300 = new Color(110, 231, 183);
        /// <summary>Emerald400 color (R:52,G:211,B:153,A:255).</summary>
        public static readonly Color Emerald400 = new Color(52, 211, 153);
        /// <summary>Emerald500 color (R:16,G:185,B:129,A:255).</summary>
        public static readonly Color Emerald500 = new Color(16, 185, 129);
        /// <summary>Emerald600 color (R:5,G:150,B:105,A:255).</summary>
        public static readonly Color Emerald600 = new Color(5, 150, 105);
        /// <summary>Emerald700 color (R:4,G:120,B:87,A:255).</summary>
        public static readonly Color Emerald700 = new Color(4, 120, 87);
        /// <summary>Emerald800 color (R:6,G:95,B:70,A:255).</summary>
        public static readonly Color Emerald800 = new Color(6, 95, 70);
        /// <summary>Emerald900 color (R:6,G:78,B:59,A:255).</summary>
        public static readonly Color Emerald900 = new Color(6, 78, 59);

        /// <summary>Teal050 color (R:240,G:253,B:250,A:255).</summary>
        public static readonly Color Teal050 = new Color(240, 253, 250);
        /// <summary>Teal100 color (R:204,G:251,B:241,A:255).</summary>
        public static readonly Color Teal100 = new Color(204, 251, 241);
        /// <summary>Teal200 color (R:153,G:246,B:228,A:255).</summary>
        public static readonly Color Teal200 = new Color(153, 246, 228);
        /// <summary>Teal300 color (R:94,G:234,B:212,A:255).</summary>
        public static readonly Color Teal300 = new Color(94, 234, 212);
        /// <summary>Teal400 color (R:45,G:212,B:191,A:255).</summary>
        public static readonly Color Teal400 = new Color(45, 212, 191);
        /// <summary>Teal500 color (R:20,G:184,B:166,A:255).</summary>
        public static readonly Color Teal500 = new Color(20, 184, 166);
        /// <summary>Teal600 color (R:13,G:148,B:136,A:255).</summary>
        public static readonly Color Teal600 = new Color(13, 148, 136);
        /// <summary>Teal700 color (R:15,G:118,B:110,A:255).</summary>
        public static readonly Color Teal700 = new Color(15, 118, 110);
        /// <summary>Teal800 color (R:17,G:94,B:89,A:255).</summary>
        public static readonly Color Teal800 = new Color(17, 94, 89);
        /// <summary>Teal900 color (R:19,G:78,B:74,A:255).</summary>
        public static readonly Color Teal900 = new Color(19, 78, 74);

        /// <summary>Cyan050 color (R:236,G:254,B:255,A:255).</summary>
        public static readonly Color Cyan050 = new Color(236, 254, 255);
        /// <summary>Cyan100 color (R:207,G:250,B:254,A:255).</summary>
        public static readonly Color Cyan100 = new Color(207, 250, 254);
        /// <summary>Cyan200 color (R:165,G:243,B:252,A:255).</summary>
        public static readonly Color Cyan200 = new Color(165, 243, 252);
        /// <summary>Cyan300 color (R:103,G:232,B:249,A:255).</summary>
        public static readonly Color Cyan300 = new Color(103, 232, 249);
        /// <summary>Cyan400 color (R:34,G:211,B:238,A:255).</summary>
        public static readonly Color Cyan400 = new Color(34, 211, 238);
        /// <summary>Cyan500 color (R:6,G:182,B:212,A:255).</summary>
        public static readonly Color Cyan500 = new Color(6, 182, 212);
        /// <summary>Cyan600 color (R:8,G:145,B:178,A:255).</summary>
        public static readonly Color Cyan600 = new Color(8, 145, 178);
        /// <summary>Cyan700 color (R:14,G:116,B:144,A:255).</summary>
        public static readonly Color Cyan700 = new Color(14, 116, 144);
        /// <summary>Cyan800 color (R:21,G:94,B:117,A:255).</summary>
        public static readonly Color Cyan800 = new Color(21, 94, 117);
        /// <summary>Cyan900 color (R:22,G:78,B:99,A:255).</summary>
        public static readonly Color Cyan900 = new Color(22, 78, 99);

        /// <summary>Sky050 color (R:240,G:249,B:255,A:255).</summary>
        public static readonly Color Sky050 = new Color(240, 249, 255);
        /// <summary>Sky100 color (R:224,G:242,B:254,A:255).</summary>
        public static readonly Color Sky100 = new Color(224, 242, 254);
        /// <summary>Sky200 color (R:186,G:230,B:253,A:255).</summary>
        public static readonly Color Sky200 = new Color(186, 230, 253);
        /// <summary>Sky300 color (R:125,G:211,B:252,A:255).</summary>
        public static readonly Color Sky300 = new Color(125, 211, 252);
        /// <summary>Sky400 color (R:56,G:189,B:248,A:255).</summary>
        public static readonly Color Sky400 = new Color(56, 189, 248);
        /// <summary>Sky500 color (R:14,G:165,B:233,A:255).</summary>
        public static readonly Color Sky500 = new Color(14, 165, 233);
        /// <summary>Sky600 color (R:2,G:132,B:199,A:255).</summary>
        public static readonly Color Sky600 = new Color(2, 132, 199);
        /// <summary>Sky700 color (R:3,G:105,B:161,A:255).</summary>
        public static readonly Color Sky700 = new Color(3, 105, 161);
        /// <summary>Sky800 color (R:7,G:89,B:133,A:255).</summary>
        public static readonly Color Sky800 = new Color(7, 89, 133);
        /// <summary>Sky900 color (R:12,G:74,B:110,A:255).</summary>
        public static readonly Color Sky900 = new Color(12, 74, 110);

        /// <summary>Blue050 color (R:239,G:246,B:255,A:255).</summary>
        public static readonly Color Blue050 = new Color(239, 246, 255);
        /// <summary>Blue100 color (R:219,G:234,B:254,A:255).</summary>
        public static readonly Color Blue100 = new Color(219, 234, 254);
        /// <summary>Blue200 color (R:191,G:219,B:254,A:255).</summary>
        public static readonly Color Blue200 = new Color(191, 219, 254);
        /// <summary>Blue300 color (R:147,G:197,B:253,A:255).</summary>
        public static readonly Color Blue300 = new Color(147, 197, 253);
        /// <summary>Blue400 color (R:96,G:165,B:250,A:255).</summary>
        public static readonly Color Blue400 = new Color(96, 165, 250);
        /// <summary>Blue500 color (R:59,G:130,B:246,A:255).</summary>
        public static readonly Color Blue500 = new Color(59, 130, 246);
        /// <summary>Blue600 color (R:37,G:99,B:235,A:255).</summary>
        public static readonly Color Blue600 = new Color(37, 99, 235);
        /// <summary>Blue700 color (R:29,G:78,B:216,A:255).</summary>
        public static readonly Color Blue700 = new Color(29, 78, 216);
        /// <summary>Blue800 color (R:30,G:64,B:175,A:255).</summary>
        public static readonly Color Blue800 = new Color(30, 64, 175);
        /// <summary>Blue900 color (R:30,G:58,B:138,A:255).</summary>
        public static readonly Color Blue900 = new Color(30, 58, 138);

        /// <summary>Indigo050 color (R:238,G:242,B:255,A:255).</summary>
        public static readonly Color Indigo050 = new Color(238, 242, 255);
        /// <summary>Indigo100 color (R:224,G:231,B:255,A:255).</summary>
        public static readonly Color Indigo100 = new Color(224, 231, 255);
        /// <summary>Indigo200 color (R:199,G:210,B:254,A:255).</summary>
        public static readonly Color Indigo200 = new Color(199, 210, 254);
        /// <summary>Indigo300 color (R:165,G:180,B:252,A:255).</summary>
        public static readonly Color Indigo300 = new Color(165, 180, 252);
        /// <summary>Indigo400 color (R:129,G:140,B:248,A:255).</summary>
        public static readonly Color Indigo400 = new Color(129, 140, 248);
        /// <summary>Indigo500 color (R:99,G:102,B:241,A:255).</summary>
        public static readonly Color Indigo500 = new Color(99, 102, 241);
        /// <summary>Indigo600 color (R:79,G:70,B:229,A:255).</summary>
        public static readonly Color Indigo600 = new Color(79, 70, 229);
        /// <summary>Indigo700 color (R:67,G:56,B:202,A:255).</summary>
        public static readonly Color Indigo700 = new Color(67, 56, 202);
        /// <summary>Indigo800 color (R:55,G:48,B:163,A:255).</summary>
        public static readonly Color Indigo800 = new Color(55, 48, 163);
        /// <summary>Indigo900 color (R:49,G:46,B:129,A:255).</summary>
        public static readonly Color Indigo900 = new Color(49, 46, 129);

        /// <summary>Violet050 color (R:245,G:243,B:255,A:255).</summary>
        public static readonly Color Violet050 = new Color(245, 243, 255);
        /// <summary>Violet100 color (R:237,G:233,B:254,A:255).</summary>
        public static readonly Color Violet100 = new Color(237, 233, 254);
        /// <summary>Violet200 color (R:221,G:214,B:254,A:255).</summary>
        public static readonly Color Violet200 = new Color(221, 214, 254);
        /// <summary>Violet300 color (R:196,G:181,B:253,A:255).</summary>
        public static readonly Color Violet300 = new Color(196, 181, 253);
        /// <summary>Violet400 color (R:167,G:139,B:250,A:255).</summary>
        public static readonly Color Violet400 = new Color(167, 139, 250);
        /// <summary>Violet500 color (R:139,G:92,B:246,A:255).</summary>
        public static readonly Color Violet500 = new Color(139, 92, 246);
        /// <summary>Violet600 color (R:124,G:58,B:237,A:255).</summary>
        public static readonly Color Violet600 = new Color(124, 58, 237);
        /// <summary>Violet700 color (R:109,G:40,B:217,A:255).</summary>
        public static readonly Color Violet700 = new Color(109, 40, 217);
        /// <summary>Violet800 color (R:91,G:33,B:182,A:255).</summary>
        public static readonly Color Violet800 = new Color(91, 33, 182);
        /// <summary>Violet900 color (R:76,G:29,B:149,A:255).</summary>
        public static readonly Color Violet900 = new Color(76, 29, 149);

        /// <summary>Purple050 color (R:245,G:243,B:255,A:255).</summary>
        public static readonly Color Purple050 = new Color(245, 243, 255);
        /// <summary>Purple100 color (R:237,G:233,B:254,A:255).</summary>
        public static readonly Color Purple100 = new Color(237, 233, 254);
        /// <summary>Purple200 color (R:221,G:214,B:254,A:255).</summary>
        public static readonly Color Purple200 = new Color(221, 214, 254);
        /// <summary>Purple300 color (R:196,G:181,B:253,A:255).</summary>
        public static readonly Color Purple300 = new Color(196, 181, 253);
        /// <summary>Purple400 color (R:167,G:139,B:250,A:255).</summary>
        public static readonly Color Purple400 = new Color(167, 139, 250);
        /// <summary>Purple500 color (R:139,G:92,B:246,A:255).</summary>
        public static readonly Color Purple500 = new Color(139, 92, 246);
        /// <summary>Purple600 color (R:124,G:58,B:237,A:255).</summary>
        public static readonly Color Purple600 = new Color(124, 58, 237);
        /// <summary>Purple700 color (R:109,G:40,B:217,A:255).</summary>
        public static readonly Color Purple700 = new Color(109, 40, 217);
        /// <summary>Purple800 color (R:91,G:33,B:182,A:255).</summary>
        public static readonly Color Purple800 = new Color(91, 33, 182);
        /// <summary>Purple900 color (R:76,G:29,B:149,A:255).</summary>
        public static readonly Color Purple900 = new Color(76, 29, 149);

        /// <summary>Fuchsia050 color (R:253,G:244,B:255,A:255).</summary>
        public static readonly Color Fuchsia050 = new Color(253, 244, 255);
        /// <summary>Fuchsia100 color (R:250,G:232,B:255,A:255).</summary>
        public static readonly Color Fuchsia100 = new Color(250, 232, 255);
        /// <summary>Fuchsia200 color (R:245,G:208,B:254,A:255).</summary>
        public static readonly Color Fuchsia200 = new Color(245, 208, 254);
        /// <summary>Fuchsia300 color (R:240,G:171,B:252,A:255).</summary>
        public static readonly Color Fuchsia300 = new Color(240, 171, 252);
        /// <summary>Fuchsia400 color (R:232,G:121,B:249,A:255).</summary>
        public static readonly Color Fuchsia400 = new Color(232, 121, 249);
        /// <summary>Fuchsia500 color (R:217,G:70,B:239,A:255).</summary>
        public static readonly Color Fuchsia500 = new Color(217, 70, 239);
        /// <summary>Fuchsia600 color (R:192,G:38,B:211,A:255).</summary>
        public static readonly Color Fuchsia600 = new Color(192, 38, 211);
        /// <summary>Fuchsia700 color (R:162,G:28,B:175,A:255).</summary>
        public static readonly Color Fuchsia700 = new Color(162, 28, 175);
        /// <summary>Fuchsia800 color (R:134,G:25,B:143,A:255).</summary>
        public static readonly Color Fuchsia800 = new Color(134, 25, 143);
        /// <summary>Fuchsia900 color (R:112,G:26,B:117,A:255).</summary>
        public static readonly Color Fuchsia900 = new Color(112, 26, 117);

        /// <summary>Pink050 color (R:253,G:242,B:248,A:255).</summary>
        public static readonly Color Pink050 = new Color(253, 242, 248);
        /// <summary>Pink100 color (R:252,G:231,B:243,A:255).</summary>
        public static readonly Color Pink100 = new Color(252, 231, 243);
        /// <summary>Pink200 color (R:251,G:207,B:232,A:255).</summary>
        public static readonly Color Pink200 = new Color(251, 207, 232);
        /// <summary>Pink300 color (R:249,G:168,B:212,A:255).</summary>
        public static readonly Color Pink300 = new Color(249, 168, 212);
        /// <summary>Pink400 color (R:244,G:114,B:182,A:255).</summary>
        public static readonly Color Pink400 = new Color(244, 114, 182);
        /// <summary>Pink500 color (R:236,G:72,B:153,A:255).</summary>
        public static readonly Color Pink500 = new Color(236, 72, 153);
        /// <summary>Pink600 color (R:219,G:39,B:119,A:255).</summary>
        public static readonly Color Pink600 = new Color(219, 39, 119);
        /// <summary>Pink700 color (R:190,G:24,B:93,A:255).</summary>
        public static readonly Color Pink700 = new Color(190, 24, 93);
        /// <summary>Pink800 color (R:157,G:23,B:77,A:255).</summary>
        public static readonly Color Pink800 = new Color(157, 23, 77);
        /// <summary>Pink900 color (R:131,G:24,B:67,A:255).</summary>
        public static readonly Color Pink900 = new Color(131, 24, 67);

        /// <summary>Rose050 color (R:255,G:241,B:242,A:255).</summary>
        public static readonly Color Rose050 = new Color(255, 241, 242);
        /// <summary>Rose100 color (R:255,G:228,B:230,A:255).</summary>
        public static readonly Color Rose100 = new Color(255, 228, 230);
        /// <summary>Rose200 color (R:254,G:205,B:211,A:255).</summary>
        public static readonly Color Rose200 = new Color(254, 205, 211);
        /// <summary>Rose300 color (R:253,G:164,B:175,A:255).</summary>
        public static readonly Color Rose300 = new Color(253, 164, 175);
        /// <summary>Rose400 color (R:251,G:113,B:133,A:255).</summary>
        public static readonly Color Rose400 = new Color(251, 113, 133);
        /// <summary>Rose500 color (R:244,G:63,B:94,A:255).</summary>
        public static readonly Color Rose500 = new Color(244, 63, 94);
        /// <summary>Rose600 color (R:225,G:29,B:72,A:255).</summary>
        public static readonly Color Rose600 = new Color(225, 29, 72);
        /// <summary>Rose700 color (R:190,G:18,B:60,A:255).</summary>
        public static readonly Color Rose700 = new Color(190, 18, 60);
        /// <summary>Rose800 color (R:159,G:18,B:57,A:255).</summary>
        public static readonly Color Rose800 = new Color(159, 18, 57);
        /// <summary>Rose900 color (R:136,G:19,B:55,A:255).</summary>
        public static readonly Color Rose900 = new Color(136, 19, 55);
    }
}
