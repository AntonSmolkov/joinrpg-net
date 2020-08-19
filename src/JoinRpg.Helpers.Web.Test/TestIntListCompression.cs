using System;
using System.Linq;
using Shouldly;
using Xunit;

namespace JoinRpg.Helpers.Web.Test
{

    public class TestIntListCompression
    {
        [Fact]
        public void Test123() => TestList(new[] { 1, 2, 3 });

        [Fact]
        public void TestEmpty() => TestList(Array.Empty<int>());

        [Fact]
        public void Test1234567() => TestList(new[] { 1, 2, 3, 4, 5, 6, 7 });

        [Fact]
        public void Test3Age()
          =>
            TestList(new[]
            {
          1358, 1359, 1360, 1396, 1413, 1420, 1719, 1720, 1754, 1755, 1756, 1759, 1760, 1761, 1764, 1766, 1767, 1768,
          1769, 1770, 1771, 1772, 1776, 1780, 1781, 1783, 1784, 1785, 1786, 1787, 1788, 1789, 1790, 1791, 1792, 1793,
          1794, 1795, 1796, 1797, 1798, 1799, 1800, 1801, 1802, 1803, 1804, 1806, 1807, 1808, 1809, 1810, 1811, 1812,
          1814, 1815, 1816, 1817, 1819, 1820, 1821, 1822, 1824, 1826, 1827, 1828, 1829, 1830, 1831, 1832, 1833, 1839,
          1842, 1843, 1847, 1853, 1854, 1855, 1856, 1857, 1861, 1862, 1863, 1864, 1865, 1866, 1867, 1868, 1869, 1871,
          1872, 1873, 1874, 1875, 1877, 1879, 1887, 1889, 1891, 1892, 1903, 1904, 1906, 1907, 1908, 1911, 1913, 1915,
          1917, 1920, 1925, 1929, 1930, 1931, 1934, 1935, 1938, 1941, 1942, 1947, 1950, 1951, 1952, 1953, 1959, 1960,
          1962, 1963, 1969, 1972, 1977, 1987, 1988, 1989, 1993, 1996, 2001, 2017, 2018, 2020, 2025, 2028, 2031, 2051,
          2064, 2066, 2074, 2077, 2080, 2083, 2094, 2098, 2099, 2101, 2105, 2109, 2112, 2113, 2117, 2124, 2135, 2137,
          2138, 2139, 2140, 2153, 2161, 2171, 2174, 2175, 2186, 2189, 2190, 2192, 2196, 2198, 2199, 2201, 2205, 2209,
          2211, 2212, 2213, 2215, 2226, 2233, 2239, 2240, 2246, 2247, 2248, 2250, 2252, 2255, 2260, 2264, 2273, 2274,
          2278, 2279, 2280, 2287, 2291, 2292, 2299, 2300, 2315, 2317, 2318, 2323, 2325, 2326, 2327, 2330, 2331, 2333,
          2335, 2337, 2338, 2341, 2342, 2346, 2347, 2348, 2349, 2350, 2351, 2352, 2353, 2354, 2357, 2359, 2360, 2362,
          2364, 2366, 2368, 2369, 2370, 2371, 2372, 2373, 2375, 2377, 2378, 2383, 2384, 2385, 2386, 2387, 2395, 2396,
          2397, 2398, 2403, 2405, 2407, 2409, 2410, 2418, 2425, 2426, 2427, 2429, 2430, 2432, 2433, 2434, 2437, 2440,
          2442, 2444, 2445, 2447, 2448, 2459, 2471, 2474, 2475, 2477, 2478, 2479, 2481, 2482, 2484, 2485, 2487, 2488,
          2490, 2493, 2497, 2498, 2499, 2500, 2501, 2503, 2506, 2507, 2508, 2509, 2510, 2511, 2512, 2513, 2515, 2518,
          2521, 2522, 2523, 2526, 2527, 2528, 2531, 2535, 2536, 2538, 2541, 2545, 2546, 2547, 2548, 2549, 2550, 2551,
          2553, 2559, 2560, 2564, 2565, 2566, 2567, 2568, 2569, 2571, 2572, 2575, 2576, 2577, 2578, 2582, 2583, 2584,
          2585, 2586, 2587, 2588, 2590, 2591, 2592, 2594, 2595, 2596, 2597, 2598, 2600, 2601, 2605, 2606, 2607, 2613,
          2614, 2615, 2619, 2621, 2622, 2627, 2628, 2629, 2630, 2631, 2632, 2633, 2635, 2636, 2638, 2639, 2640, 2641,
          2642, 2643, 2644, 2646, 2648, 2649, 2650, 2659, 2660, 2662, 2663, 2664, 2665, 2667, 2668, 2670, 2673, 2675,
          2681, 2682, 2683, 2685, 2692, 2694, 2695, 2698, 2699, 2701, 2702, 2703, 2704, 2705, 2706, 2709, 2711, 2712,
          2713, 2714, 2719, 2720, 2721, 2722, 2723, 2724, 2729, 2730, 2731, 2732, 2733, 2734, 2736, 2737, 2738, 2739,
          2741, 2745, 2746, 2747, 2751, 2755, 2756, 2757, 2758, 2759, 2761, 2765, 2771, 2774, 2775, 2777, 2778, 2780,
          2781, 2784, 2787, 2792, 2793, 2794, 2795, 2798, 2803, 2804, 2805, 2806, 2808, 2815, 2821, 2822, 2824, 2828,
          2830, 2834, 2835, 2840, 2841, 2844, 2845, 2846, 2848, 2849, 2850, 2851, 2855, 2857, 2858, 2859, 2860, 2861,
          2862, 2863, 2864, 2868, 2869, 2870, 2873, 2874, 2875, 2876, 2887, 2901, 2904, 2909, 2912, 2913, 2920, 2922,
          2923, 2925, 2926, 2927, 2928, 2930, 2933, 2934, 2936, 2937, 2938, 2940, 2942, 2943, 2944, 2946, 2947, 2949,
          2950, 2954, 2955, 2957, 2958, 2959, 2962, 2963, 2965, 2967, 2968, 2970, 2971, 2972, 2973, 2974, 2978, 2979,
          2980, 2981, 2983, 2984, 2985, 2986, 2987, 2989, 2990, 2993, 2994, 2995, 2996, 2997, 3000, 3002, 3003, 3004,
          3012, 3014, 3015, 3016, 3017, 3019, 3020, 3021, 3023, 3024, 3025, 3026, 3029, 3031, 3032, 3037, 3038, 3040,
          3041, 3042, 3043, 3044, 3045, 3047, 3048, 3049, 3065, 3113, 3119, 3125, 3126, 3137, 3142, 3144, 3152, 3167,
          3168, 3169, 3170, 3172, 3179, 3181, 3182, 3183, 3188, 3190, 3192, 3194, 3197, 3198, 3207, 3208, 3209, 3212,
          3213, 3218, 3226, 3228, 3231, 3234, 3236, 3237, 3240, 3242, 3243, 3245, 3246, 3247, 3250, 3251, 3253, 3254,
          3255, 3256, 3261, 3262, 3263, 3264, 3266, 3267, 3270, 3272, 3273, 3275, 3278, 3280, 3281, 3290, 3299, 3303,
          3306, 3307, 3309, 3310, 3313, 3319, 3320, 3322, 3323, 3333, 3334, 3335, 3337, 3339, 3341, 3342, 3345, 3350,
          3352, 3354, 3368, 3369, 3371, 3373, 3375, 3385, 3388, 3389, 3391, 3400, 3405, 3406, 3407, 3411, 3414, 3418,
          3422, 3426, 3428, 3429, 3435, 3436, 3437, 3440, 3441, 3442, 3443, 3444, 3447, 3448, 3449, 3450, 3451, 3452,
          3453, 3454, 3455, 3456, 3457, 3458, 3459, 3460, 3461, 3462, 3463, 3467, 3469, 3472, 3473, 3474, 3477, 3478,
          3479, 3480, 3481, 3482, 3484, 3485, 3487, 3488, 3489, 3491, 3492, 3494, 3495, 3496, 3497, 3498, 3500, 3501,
          3502, 3503, 3504, 3506, 3507, 3508, 3510, 3512, 3513, 3517, 3518, 3519, 3521, 3523, 3524, 3528, 3529, 3532,
          3536, 3541, 3542, 3543, 3544, 3546, 3547, 3548, 3550, 3551, 3552, 3559, 3560, 3562, 3563, 3566, 3568, 3569,
          3570, 3571, 3573, 3574, 3576, 3582, 3583, 3585, 3586, 3587, 3588, 3589, 3590, 3591, 3593, 3595, 3596, 3597,
          3598, 3599, 3600, 3601, 3602, 3603, 3610, 3611, 3614, 3616, 3619, 3620, 3621, 3622, 3626, 3627, 3628, 3629,
          3633, 3635, 3637, 3639, 3642, 3643, 3649, 3652, 3653, 3654, 3655, 3658, 3661, 3666, 3676, 3678, 3679, 3681,
          3687, 3690, 3691, 3692, 3693, 3699, 3700, 3701, 3702, 3705, 3707, 3708, 3709, 3710, 3714, 3715, 3716, 3717,
          3718, 3719, 3721, 3722, 3724, 3726, 3727, 3728, 3729, 3734, 3735, 3738, 3739, 3740, 3741, 3743, 3745, 3747,
          3748, 3749, 3750, 3752, 3753, 3754, 3755, 3756, 3759, 3760, 3761, 3762, 3763, 3764, 3765, 3767, 3769, 3775,
          3786, 3787, 3789, 3791, 3792, 3793, 3794, 3796, 3801, 3805, 3813, 3814, 3819, 3821, 3824, 3826, 3827, 3829,
          3830, 3831, 3832, 3835, 3836, 3837, 3838, 3839, 3840, 3841, 3842, 3843, 3847, 3848, 3849, 3850, 3853, 3862,
          3864, 3870, 3872, 3873, 3874, 3875, 3883, 3888, 3890, 3891, 3894, 3896, 3897, 3898, 3899, 3900, 3905, 3907,
          3911, 3913, 3914, 3918, 3920, 3922, 3925, 3927, 3928, 3929, 3930, 3931, 3933, 3934, 3935, 3936, 3937, 3938,
          3939, 3940, 3942, 3946, 3947, 3948, 3949, 3952, 3953, 3954, 3956, 3958, 3960, 3961, 3962, 3963, 3964, 3965,
          3966, 3970, 3972, 3974, 3979, 3988, 3989, 3991, 3992, 3993, 3994, 3995, 3997, 3998, 3999, 4000, 4002, 4008,
          4010, 4015, 4016, 4017, 4018, 4020, 4023, 4024, 4025, 4026, 4027, 4028, 4031, 4032, 4035, 4037, 4041, 4043,
          4045, 4046, 4047, 4048, 4050, 4051, 4054, 4055, 4057, 4058, 4061, 4075, 4076, 4077, 4080, 4087, 4094, 4101,
          4109, 4117, 4121, 4122, 4124, 4127, 4133, 4140, 4141, 4144, 4147, 4149, 4150, 4156, 4158, 4159, 4160, 4161,
          4166, 4167, 4174, 4181, 4183, 4185, 4186, 4187, 4189, 4190, 4192, 4194, 4201, 4202, 4211, 4212, 4215, 4219,
          4231, 4232, 4233, 4235, 4237, 4239, 4240, 4250, 4253, 4254, 4256, 4257, 4258, 4259, 4260, 4261, 4262, 4263,
          4264, 4268, 4269, 4271, 4273, 4274, 4276, 4279, 4284, 4285, 4286, 4287, 4290, 4294, 4295, 4296, 4300, 4306,
          4307, 4308, 4309, 4323, 4372, 4381, 4392, 4398, 4411, 4413, 4440, 4445, 4450, 4454, 4455, 4461, 4470, 4473,
          4487, 4492, 4494, 4499, 4500, 4505, 4507, 4508, 4510, 4516, 4520, 4523, 4524, 4526, 4527, 4528, 4529, 4530,
          4531, 4550, 4556, 4579, 4584, 4585, 4595, 4598, 4599, 4604, 4607, 4610, 4614, 4627, 4635, 4642, 4654, 4656,
          4668, 4669, 4677, 4679, 4689, 4692, 4700, 4701, 4702, 4722, 4738, 4745, 4749, 4750, 4751, 4768, 4772, 4774,
          4778, 4793, 4795, 4800, 4805, 4809, 4810, 4812, 4813, 4814, 4815, 4818, 4828, 4834, 4835, 4836, 4837, 4838,
          4839, 4842, 4843, 4845, 4846, 4849, 4852, 4853, 4854, 4855, 4856, 4859, 4861, 4862, 4863, 4864, 4865, 4869,
          4871, 4872, 4876, 4877, 4878, 4879, 4882, 4883, 4884, 4885, 4888, 4889, 4896, 4897, 4899, 4905, 4906, 4907,
          4908, 4909, 4925, 4927, 4929, 4935, 4936, 4940, 4946, 4947, 4948, 4951, 4953, 4956, 4957, 4967, 4968, 4976,
          4978, 4980, 4981, 4982, 4983, 4987, 4989, 4992, 5001, 5004, 5008, 5009, 5010, 5013, 5014, 5020, 5030, 5034,
          5035, 5036, 5037, 5038, 5040, 5045, 5051, 5053, 5054, 5064, 5071, 5075, 5096, 5099, 5100, 5107, 5114, 5116,
          5130, 5131, 5132, 5143, 5147, 5152, 5153, 5156, 5157, 5158, 5159, 5171, 5173, 5175, 5178, 5179, 5180, 5181,
          5182, 5183, 5184, 5188, 5189, 5190, 5195, 5199, 5203, 5205, 5207, 5210, 5218, 5225, 5226, 5227, 5228, 5229,
          5230, 5231, 5236, 5248, 5249, 5263, 5264, 5266, 5267, 5268, 5278, 5280, 5283, 5284, 5285, 5286, 5287, 5292,
          5293, 5295, 5306, 5321, 5335, 5340, 5341, 5342, 5343, 5345, 5350, 5352, 5356, 5357, 5375, 5387, 5396, 5410,
          5413, 5449, 5450, 5461, 5469, 5496, 5498, 5499, 5503, 5505, 5509, 5510, 5515, 5517, 5518, 5535, 5539, 5546,
          5557, 5559, 5563, 5565, 5566, 5567, 5572, 5573, 5574, 5584, 5586, 5591, 5629, 5630, 5631, 5632, 5633, 5637,
          5639, 5641, 5642, 5643, 5651, 5661, 5666, 5670, 5676, 5677, 5679, 5682, 5683, 5685, 5693, 5695, 5703, 5704,
          5712, 5720, 5730, 5737, 5746, 5747, 5748, 5749, 5750, 5753, 5756, 5759, 5760, 5761, 5762, 5763, 5765, 5767,
          5770, 5775, 5782, 5783, 5784, 5786, 5787, 5808, 5852, 5853, 5855, 5896, 5908, 5918, 5920, 5922, 5923, 5926,
          5933, 5942, 5952, 5955, 5959, 5964, 5965, 5966, 5968, 5977, 5983, 5985, 5995, 6000, 6007, 6016, 6031, 6034,
          6043, 6045, 6061, 6063, 6072, 6079, 6083, 6090, 6091, 6092, 6095, 6098, 6099, 6110, 6111, 6113, 6115, 6116,
          6119, 6121, 6133, 6152, 6157, 6160, 6162, 6166, 6171, 6172, 6181, 6182, 6200, 6227, 6241, 6243, 6257, 6259,
          6276, 6280, 6283, 6284, 6288, 6290, 6291, 6307, 6308, 6374, 6377, 6379, 6381, 6382, 6383, 6384, 6398, 6399,
          6403, 6410, 6411, 6414, 6419, 6435, 6453, 6455, 6469, 6470, 6483, 6490, 6513, 6520, 6525, 6535, 6541, 6549,
          6552, 6555, 6556, 6557, 6559, 6560, 6561, 6562, 6563, 6564, 6565, 6568, 6577, 6586, 6587, 6588, 6590, 6595,
          6617, 6630, 6649, 6653, 6655, 6656, 6659, 6672, 6701, 6712, 6721, 6738, 6742, 6753, 6755, 6760, 6770, 6771,
          6777, 6782, 6796, 6831, 6836, 6848, 6870, 6915, 6919, 6953,
            });

        private static void TestList(int[] list)
        {
            var compressed = list.CompressIdList();
            var result = compressed.UnCompressIdList().ToArray();
            result.ShouldBe(list);
        }
    }
}