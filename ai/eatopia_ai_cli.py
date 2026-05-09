import argparse
import hashlib
import json
import os
import random
import re
import sys
import time
from pathlib import Path


ROOT = Path(__file__).resolve().parent
os.chdir(ROOT)
sys.path.insert(0, str(ROOT))


class Profile:
    def __init__(self, age, weight, height, activity, goal, meals_per_day):
        self.age = age
        self.weight = weight
        self.height = height
        self.activity = activity
        self.goal = goal
        self.meals_per_day = meals_per_day


EGYPTIAN_MEALS = {
    "breakfast": [
        {
            "food": "ful_medames_bowl",
            "recipe_name": "Ful Medames Bowl",
            "recipe_search": "Ful Medames Bowl",
            "calories": 330,
            "protein": 18,
            "carbs": 46,
            "fat": 8,
            "goals": {"lose_weight", "maintain", "gain_muscle"},
        },
        {
            "food": "taameya_salad_plate",
            "recipe_name": "Taameya Salad Plate",
            "recipe_search": "Taameya Salad Plate",
            "calories": 360,
            "protein": 16,
            "carbs": 42,
            "fat": 13,
            "goals": {"lose_weight", "maintain", "gain_muscle"},
        },
        {
            "food": "egyptian_egg_tomato_skillet",
            "recipe_name": "Egyptian Egg Tomato Skillet",
            "recipe_search": "Egyptian Egg Tomato Skillet",
            "calories": 300,
            "protein": 20,
            "carbs": 24,
            "fat": 12,
            "goals": {"lose_weight", "maintain"},
        },
        {
            "food": "low_fat_cheese_baladi_plate",
            "recipe_name": "Low-Fat Cheese Baladi Plate",
            "recipe_search": "Low-Fat Cheese Baladi Plate",
            "calories": 290,
            "protein": 21,
            "carbs": 35,
            "fat": 7,
            "goals": {"lose_weight", "maintain"},
        },
        {
            "food": "ful_with_eggs_plate",
            "recipe_name": "Ful with Eggs",
            "recipe_search": "Ful with Eggs",
            "calories": 390,
            "protein": 24,
            "carbs": 44,
            "fat": 12,
            "goals": {"lose_weight", "maintain", "gain_muscle"},
        },
        {
            "food": "eggah_slice",
            "recipe_name": "Eggah",
            "recipe_search": "Eggah",
            "calories": 280,
            "protein": 18,
            "carbs": 22,
            "fat": 12,
            "goals": {"lose_weight", "maintain"},
        },
        {
            "food": "potato_eggah_plate",
            "recipe_name": "Potato Eggah",
            "recipe_search": "Potato Eggah",
            "calories": 340,
            "protein": 17,
            "carbs": 38,
            "fat": 13,
            "goals": {"lose_weight", "maintain", "gain_muscle"},
        },
        {
            "food": "mish_cheese_baladi_plate",
            "recipe_name": "Mish Cheese Plate",
            "recipe_search": "Mish Cheese Plate",
            "calories": 310,
            "protein": 20,
            "carbs": 32,
            "fat": 11,
            "goals": {"lose_weight", "maintain"},
        },
    ],
    "lunch": [
        {
            "food": "molokhia_chicken_bowl",
            "recipe_name": "Molokhia Chicken Bowl",
            "recipe_search": "Molokhia Chicken Bowl",
            "calories": 470,
            "protein": 38,
            "carbs": 44,
            "fat": 14,
            "goals": {"lose_weight", "maintain", "gain_muscle"},
        },
        {
            "food": "light_koshari_bowl",
            "recipe_name": "Light Koshari Bowl",
            "recipe_search": "Light Koshari Bowl",
            "calories": 520,
            "protein": 22,
            "carbs": 78,
            "fat": 10,
            "goals": {"lose_weight", "maintain", "gain_muscle"},
        },
        {
            "food": "grilled_kofta_rice_plate",
            "recipe_name": "Grilled Kofta Rice Plate",
            "recipe_search": "Grilled Kofta Rice Plate",
            "calories": 540,
            "protein": 36,
            "carbs": 56,
            "fat": 18,
            "goals": {"lose_weight", "maintain", "gain_muscle"},
        },
        {
            "food": "tuna_baladi_salad",
            "recipe_name": "Tuna Baladi Salad",
            "recipe_search": "Tuna Baladi Salad",
            "calories": 350,
            "protein": 34,
            "carbs": 28,
            "fat": 10,
            "goals": {"lose_weight", "maintain"},
        },
        {
            "food": "molokhia_rabbit_bowl",
            "recipe_name": "Molokhia with Rabbit",
            "recipe_search": "Molokhia with Rabbit",
            "calories": 500,
            "protein": 39,
            "carbs": 46,
            "fat": 16,
            "goals": {"lose_weight", "maintain", "gain_muscle"},
        },
        {
            "food": "kofta_tomato_plate",
            "recipe_name": "Kofta in Tomato Sauce",
            "recipe_search": "Kofta in Tomato Sauce",
            "calories": 510,
            "protein": 34,
            "carbs": 42,
            "fat": 20,
            "goals": {"lose_weight", "maintain", "gain_muscle"},
        },
        {
            "food": "potato_tray_chicken_plate",
            "recipe_name": "Potato Tray with Chicken",
            "recipe_search": "Potato Tray with Chicken",
            "calories": 490,
            "protein": 36,
            "carbs": 48,
            "fat": 15,
            "goals": {"lose_weight", "maintain", "gain_muscle"},
        },
        {
            "food": "mixed_mahshi_tray_light",
            "recipe_name": "Mixed Mahshi Tray",
            "recipe_search": "Mixed Mahshi Tray",
            "calories": 460,
            "protein": 15,
            "carbs": 72,
            "fat": 12,
            "goals": {"lose_weight", "maintain"},
        },
    ],
    "dinner": [
        {
            "food": "egyptian_lentil_soup",
            "recipe_name": "Egyptian Lentil Soup",
            "recipe_search": "Egyptian Lentil Soup",
            "calories": 320,
            "protein": 18,
            "carbs": 48,
            "fat": 7,
            "goals": {"lose_weight", "maintain"},
        },
        {
            "food": "eggplant_mesakaa_plate",
            "recipe_name": "Eggplant Mesakaa Plate",
            "recipe_search": "Eggplant Mesakaa Plate",
            "calories": 340,
            "protein": 14,
            "carbs": 34,
            "fat": 16,
            "goals": {"lose_weight", "maintain"},
        },
        {
            "food": "sayadeya_fish_plate",
            "recipe_name": "Sayadeya Fish Plate",
            "recipe_search": "Sayadeya Fish Plate",
            "calories": 430,
            "protein": 34,
            "carbs": 48,
            "fat": 11,
            "goals": {"lose_weight", "maintain", "gain_muscle"},
        },
        {
            "food": "vegetable_torly_bowl",
            "recipe_name": "Vegetable Torly Bowl",
            "recipe_search": "Vegetable Torly Bowl",
            "calories": 310,
            "protein": 12,
            "carbs": 42,
            "fat": 10,
            "goals": {"lose_weight", "maintain"},
        },
        {
            "food": "baladi_salad_cheese_plate",
            "recipe_name": "Cheese with Tomato Baladi Plate",
            "recipe_search": "Cheese with Tomato Baladi Plate",
            "calories": 260,
            "protein": 19,
            "carbs": 25,
            "fat": 8,
            "goals": {"lose_weight", "maintain"},
        },
        {
            "food": "cauliflower_tomato_plate",
            "recipe_name": "Cauliflower with Tomato Sauce",
            "recipe_search": "Cauliflower with Tomato Sauce",
            "calories": 280,
            "protein": 11,
            "carbs": 35,
            "fat": 10,
            "goals": {"lose_weight", "maintain"},
        },
        {
            "food": "egyptian_potato_salad_plate",
            "recipe_name": "Egyptian Potato Salad",
            "recipe_search": "Egyptian Potato Salad",
            "calories": 300,
            "protein": 7,
            "carbs": 46,
            "fat": 9,
            "goals": {"maintain"},
        },
        {
            "food": "lentil_fatta_light",
            "recipe_name": "Lentil Fatta",
            "recipe_search": "Lentil Fatta",
            "calories": 360,
            "protein": 17,
            "carbs": 58,
            "fat": 7,
            "goals": {"lose_weight", "maintain"},
        },
    ],
    "snacks": [
        {
            "food": "dates_greek_yogurt",
            "recipe_name": "Dates Greek Yogurt Cup",
            "recipe_search": "Dates Greek Yogurt Cup",
            "calories": 210,
            "protein": 14,
            "carbs": 30,
            "fat": 4,
            "goals": {"lose_weight", "maintain", "gain_muscle"},
        },
        {
            "food": "roasted_chickpeas_cucumber",
            "recipe_name": "Roasted Chickpeas Cucumber Snack",
            "recipe_search": "Roasted Chickpeas Cucumber Snack",
            "calories": 190,
            "protein": 9,
            "carbs": 28,
            "fat": 5,
            "goals": {"lose_weight", "maintain"},
        },
        {
            "food": "belila_cup",
            "recipe_name": "Belila",
            "recipe_search": "Belila",
            "calories": 230,
            "protein": 9,
            "carbs": 42,
            "fat": 4,
            "goals": {"lose_weight", "maintain", "gain_muscle"},
        },
        {
            "food": "sweet_potato_oven_dessert",
            "recipe_name": "Sweet Potato Oven Dessert",
            "recipe_search": "Sweet Potato Oven Dessert",
            "calories": 240,
            "protein": 4,
            "carbs": 50,
            "fat": 2,
            "goals": {"lose_weight", "maintain"},
        },
        {
            "food": "date_milk",
            "recipe_name": "Date Milk",
            "recipe_search": "Date Milk",
            "calories": 220,
            "protein": 8,
            "carbs": 38,
            "fat": 5,
            "goals": {"lose_weight", "maintain", "gain_muscle"},
        },
        {
            "food": "rice_pudding_light",
            "recipe_name": "Rice Pudding",
            "recipe_search": "Rice Pudding",
            "calories": 250,
            "protein": 8,
            "carbs": 45,
            "fat": 5,
            "goals": {"lose_weight", "maintain", "gain_muscle"},
        },
        {
            "food": "baladi_salad_snack",
            "recipe_name": "Baladi Salad",
            "recipe_search": "Baladi Salad",
            "calories": 120,
            "protein": 4,
            "carbs": 18,
            "fat": 4,
            "goals": {"lose_weight", "maintain"},
        },
    ],
}


def read_payload():
    raw = sys.stdin.read().strip()
    if not raw:
        return {}
    return json.loads(raw)


def pick(data, *keys, default=None):
    for key in keys:
        if isinstance(data, dict) and key in data:
            return data[key]
    return default


def normalize_goal(value):
    text = str(value or "").strip().lower().replace("-", "_").replace(" ", "_")
    if text in {"lose_weight", "weight_loss", "fat_loss", "cut", "slim"}:
        return "lose_weight"
    if text in {"gain_muscle", "muscle_gain", "bulk", "build_muscle"}:
        return "gain_muscle"
    return "maintain"


def normalize_activity(value):
    text = str(value or "").strip().lower()
    if text in {"sedentary", "light", "moderate", "active"}:
        return text
    return "moderate"


def to_number(value, fallback):
    try:
        if value is None or value == "":
            return fallback
        return float(value)
    except (TypeError, ValueError):
        return fallback


def humanize_food_name(name):
    text = str(name or "Balanced meal").strip().lower()
    text = re.sub(r"_(\d+)$", "", text)
    parts = [
        part
        for part in text.replace("-", "_").split("_")
        if part and part not in {"breakfast", "lunch", "dinner", "snack", "snacks", "meal"}
    ]
    return " ".join(parts or ["balanced", "meal"]).title()


def meal_signature(name):
    text = str(name or "").strip().lower()
    text = re.sub(r"_(\d+)$", "", text)
    parts = [
        part
        for part in text.replace("-", "_").split("_")
        if part and part not in {"breakfast", "lunch", "dinner", "snack", "snacks", "meal"}
    ]
    return "_".join(parts)


def contains_avoided_food(text, avoided):
    lower = str(text or "").lower()
    return any(item and item in lower for item in avoided)


def meal_text(item):
    name = humanize_food_name(item.get("food"))
    calories = round(float(item.get("calories", 0)))
    protein = round(float(item.get("protein", 0)))
    carbs = round(float(item.get("carbs", 0)))
    fat = round(float(item.get("fat", 0)))

    return "\n".join(
        [
            name,
            f"{calories} cal",
            f"Protein {protein}g",
            f"Carbs {carbs}g",
            f"Fat {fat}g",
        ]
    )


def nearest_recipe_search(food_name, used_recipe_searches=None):
    used_recipe_searches = used_recipe_searches if used_recipe_searches is not None else set()
    text = str(food_name or "").lower()
    rules = [
        (("molokhia", "chicken"), "Molokhia Chicken Bowl"),
        (("koshari",), "Light Koshari Bowl"),
        (("ful", "fava"), "Ful Medames Bowl"),
        (("taameya", "falafel"), "Taameya Salad Plate"),
        (("lentil",), "Egyptian Lentil Soup"),
        (("eggplant",), "Eggplant Mesakaa Plate"),
        (("cottage", "cheese", "mish"), "Low-Fat Cheese Baladi Plate"),
        (("mahshi", "stuffed"), "Mixed Mahshi Tray"),
        (("potato", "tray"), "Potato Tray with Chicken"),
        (("tuna",), "Tuna Baladi Salad"),
        (("kofta", "beef"), "Grilled Kofta Rice Plate"),
        (("fish", "salmon", "tilapia"), "Sayadeya Fish Plate"),
        (("belila",), "Belila"),
        (("sweet", "dessert"), "Sweet Potato Oven Dessert"),
        (("date", "milk"), "Date Milk"),
        (("rice", "pudding"), "Rice Pudding"),
        (("chickpea",), "Roasted Chickpeas Cucumber Snack"),
        (("yogurt",), "Greek Yogurt Parfait"),
        (("egg",), "Egyptian Egg Tomato Skillet"),
        (("chicken",), "Lemon Herb Grilled Chicken"),
        (("tofu",), "Grilled Tofu Salad"),
        (("salad", "vegetable", "zucchini", "broccoli", "spinach"), "Vegetable Torly Bowl"),
    ]

    candidates = []
    for keywords, recipe in rules:
        if any(keyword in text for keyword in keywords):
            candidates.append(recipe)

    candidates.extend(
        [
            humanize_food_name(food_name),
            "Ful Medames Bowl",
            "Molokhia Chicken Bowl",
            "Tuna Baladi Salad",
            "Egyptian Lentil Soup",
            "Vegetable Torly Bowl",
            "Sayadeya Fish Plate",
            "Light Koshari Bowl",
        ]
    )

    seen = set()
    for candidate in candidates:
        if not candidate or candidate in seen:
            continue
        seen.add(candidate)
        if candidate not in used_recipe_searches:
            used_recipe_searches.add(candidate)
            return candidate

    fallback = f"{humanize_food_name(food_name)} {len(used_recipe_searches) + 1}"
    used_recipe_searches.add(fallback)
    return fallback


def meal_response(title, item, used_recipe_searches):
    preferred_recipe = item.get("recipe_name")
    if preferred_recipe:
        recipe_name = preferred_recipe
        used_recipe_searches.add(recipe_name)
    else:
        recipe_name = nearest_recipe_search(item.get("food"), used_recipe_searches)

    return {
        "title": title,
        "text": meal_text(item),
        "recipeName": recipe_name,
        "recipeSearch": recipe_name,
        "calories": round(float(item.get("calories", 0))),
        "protein": round(float(item.get("protein", 0))),
        "carbs": round(float(item.get("carbs", 0))),
        "fat": round(float(item.get("fat", 0))),
    }


def activity_multiplier(activity_level):
    return {
        "sedentary": 1.2,
        "light": 1.375,
        "moderate": 1.55,
        "active": 1.725,
    }.get(activity_level, 1.55)


def build_weight_forecast(weight, goal, activity_level, target_calories, weeks=8):
    current_weight = max(float(weight or 70), 1)
    tdee = current_weight * 22 * activity_multiplier(activity_level)
    daily_delta = float(target_calories or tdee) - tdee
    weekly_change = (daily_delta * 7) / 7700

    if goal == "lose_weight":
        weekly_change = min(-0.25, max(weekly_change, -1.25))
        direction = "loss"
    elif goal == "gain_muscle":
        weekly_change = max(0.15, min(weekly_change, 0.65))
        direction = "gain"
    else:
        weekly_change = 0
        direction = "stable"

    forecast = []
    for week in range(1, weeks + 1):
        total_change = weekly_change * week
        expected_weight = max(current_weight + total_change, 1)
        forecast.append(
            {
                "week": week,
                "expectedWeightKg": round(expected_weight, 1),
                "weeklyChangeKg": round(weekly_change, 1),
                "totalChangeKg": round(total_change, 1),
                "expectedLossKg": round(abs(total_change) if total_change < 0 else 0, 1),
                "direction": direction,
            }
        )

    return forecast


def choose_clean_item(goal, candidates, avoided, used_signatures=None):
    used_signatures = used_signatures if used_signatures is not None else set()

    for item in candidates:
        signature = meal_signature(item.get("food"))
        if (
            not contains_avoided_food(item.get("food"), avoided)
            and signature not in used_signatures
        ):
            used_signatures.add(signature)
            return item

    from engine.food_generator import create_meal

    for _ in range(80):
        item = create_meal(goal, "meal")
        signature = meal_signature(item.get("food"))
        if (
            not contains_avoided_food(item.get("food"), avoided)
            and signature not in used_signatures
        ):
            used_signatures.add(signature)
            return item

    fallback = {
        "food": "lean_protein_vegetables",
        "calories": 420,
        "protein": 32,
        "carbs": 36,
        "fat": 14,
    }
    used_signatures.add(meal_signature(fallback["food"]))
    return fallback


def egyptian_candidates(goal, meal_type):
    choices = [
        dict(item)
        for item in EGYPTIAN_MEALS.get(meal_type, [])
        if goal in item.get("goals", set())
    ]
    random.shuffle(choices)
    return choices


def with_egyptian_candidates(goal, meal_type, candidates):
    local_candidates = egyptian_candidates(goal, meal_type)
    if not local_candidates:
        return candidates

    return local_candidates + list(candidates or [])


def choose_clean_snack(goal, candidate, avoided, used_signatures):
    from engine.food_generator import create_meal

    candidate_items = egyptian_candidates(goal, "snacks")
    if candidate:
        candidate_items.append(candidate)
    for _ in range(80):
        candidate_items.append(create_meal(goal, "snack"))

    candidate_items.extend(
        [
            {
                "food": "apple_yogurt",
                "calories": 190,
                "protein": 11,
                "carbs": 28,
                "fat": 4,
            },
            {
                "food": "orange_almonds",
                "calories": 210,
                "protein": 8,
                "carbs": 22,
                "fat": 10,
            },
            {
                "food": "cucumber_hummus",
                "calories": 170,
                "protein": 7,
                "carbs": 20,
                "fat": 7,
            },
        ]
    )

    for item in candidate_items:
        signature = meal_signature(item.get("food"))
        if (
            signature
            and signature not in used_signatures
            and not contains_avoided_food(item.get("food"), avoided)
        ):
            used_signatures.add(signature)
            return item

    fallback = {
        "food": "fruit_yogurt",
        "calories": 180,
        "protein": 10,
        "carbs": 24,
        "fat": 4,
    }
    used_signatures.add(meal_signature(fallback["food"]))
    return fallback


def seed_generation(payload):
    requested_seed = pick(payload, "seed", "Seed", "generationId", "GenerationId")
    if requested_seed not in (None, ""):
        random.seed(str(requested_seed))
        return "request-seed"

    random.seed(
        f"{time.time_ns()}:{os.getpid()}:{random.SystemRandom().getrandbits(96)}"
    )
    return "fresh-random"


def build_diet_plan(payload):
    from engine.macros import calculate_macros
    from engine.meal_generator import generate_day
    from engine.food_generator import create_meal

    preferences = pick(payload, "preferences", "Preferences", default={}) or {}
    goal = normalize_goal(
        pick(payload, "goal", "Goal", default=pick(preferences, "goal", "Goal"))
    )
    activity = normalize_activity(pick(payload, "activity", "Activity", "activityLevel", "ActivityLevel"))
    age = int(to_number(pick(payload, "age", "Age"), 30))
    weight = to_number(pick(payload, "weight", "Weight", "weightKg", "WeightKg"), 70)
    height = to_number(pick(payload, "height", "Height", "heightCm", "HeightCm"), 170)
    duration_days = int(to_number(pick(payload, "durationDays", "DurationDays"), 7))
    duration_days = max(1, min(duration_days, 14))

    meal_names = pick(payload, "mealsPerDay", "MealsPerDay", default=None)
    meals_per_day = len(meal_names) if isinstance(meal_names, list) and meal_names else 4

    avoided = set()
    for group in (
        pick(payload, "avoidFoods", "AvoidFoods", default=[]),
        pick(payload, "allergies", "Allergies", default=[]),
    ):
        for item in group or []:
            value = str(item or "").strip().lower()
            if value:
                avoided.add(value)

    profile = Profile(age, weight, height, activity, goal, meals_per_day)
    target = calculate_macros(weight, goal, activity, age=age, height=height)
    weight_forecast = build_weight_forecast(weight, goal, activity, target.get("calories"))

    weekly_plan = []
    seed_source = seed_generation(payload)
    used_signatures = set()
    used_recipe_searches = set()

    for day in range(1, duration_days + 1):
        meals = generate_day(profile)
        snack = create_meal(goal, "snack")
        snack = choose_clean_snack(goal, snack, avoided, used_signatures)

        breakfast = choose_clean_item(goal, with_egyptian_candidates(goal, "breakfast", meals.get("breakfast", [])), avoided, used_signatures)
        lunch = choose_clean_item(goal, with_egyptian_candidates(goal, "lunch", meals.get("lunch", [])), avoided, used_signatures)
        dinner = choose_clean_item(goal, with_egyptian_candidates(goal, "dinner", meals.get("dinner", [])), avoided, used_signatures)

        weekly_plan.append(
            {
                "day": day,
                "meals": {
                    "breakfast": meal_response("Breakfast", breakfast, used_recipe_searches),
                    "lunch": meal_response("Lunch", lunch, used_recipe_searches),
                    "dinner": meal_response("Dinner", dinner, used_recipe_searches),
                    "snacks": meal_response("Snacks", snack, used_recipe_searches),
                },
            }
        )

    return {
        "weeklyPlan": weekly_plan,
        "targetMacros": target,
        "weightForecast": weight_forecast,
        "source": f"python-ai-engine:{seed_source}",
    }


def estimate_nutrition(food_name, ingredients=None):
    ingredients = ingredients or []
    text = " ".join([food_name, *ingredients]).lower()

    calories = 460
    protein = 24
    carbs = 48
    fat = 16
    fiber = 6
    sugar = 7

    rules = [
        (("pizza", "cheese"), (280, 6, 28, 14, -1, 2)),
        (("burger", "beef"), (240, 16, 20, 13, -1, 2)),
        (("pasta", "spaghetti", "noodle"), (170, 2, 36, 4, 1, 3)),
        (("rice", "biryani"), (150, 2, 35, 2, 0, 0)),
        (("chicken", "turkey"), (80, 18, -8, 2, 0, 0)),
        (("fish", "salmon", "tuna"), (70, 16, -10, 6, 0, 0)),
        (("salad", "lettuce", "cucumber", "spinach"), (-190, -6, -24, -8, 4, -2)),
        (("cake", "dessert", "chocolate"), (260, -6, 38, 12, -2, 22)),
        (("egg", "omelet"), (70, 10, -12, 7, 0, 0)),
        (("beans", "lentil", "chickpea"), (40, 8, 16, -3, 6, 0)),
    ]

    for keywords, delta in rules:
        if any(keyword in text for keyword in keywords):
            calories += delta[0]
            protein += delta[1]
            carbs += delta[2]
            fat += delta[3]
            fiber += delta[4]
            sugar += delta[5]

    return {
        "calories": int(max(90, min(calories, 1400))),
        "protein": int(max(2, min(protein, 120))),
        "carbs": int(max(2, min(carbs, 180))),
        "fat": int(max(1, min(fat, 90))),
        "fiber": int(max(0, min(fiber, 35))),
        "sugar": int(max(0, min(sugar, 90))),
    }


def zero_nutrition():
    return {
        "calories": 0,
        "protein": 0,
        "carbs": 0,
        "fat": 0,
        "fiber": 0,
        "sugar": 0,
    }


def non_food_result(reason):
    return {
        "isFood": False,
        "foodName": "Not a food image",
        "confidence": 0,
        **zero_nutrition(),
        "ingredients": [],
        "instructions": [],
        "source": "image-guard",
        "note": reason,
        "message": "This image does not look like food. Please upload a clear meal or ingredient photo.",
    }


def _count_mask_components(mask):
    import numpy as np

    height, width = mask.shape
    seen = np.zeros(mask.shape, dtype=bool)
    components = []

    for y in range(height):
        starts = np.where(mask[y] & ~seen[y])[0]
        for start_x in starts:
            if seen[y, start_x] or not mask[y, start_x]:
                continue

            stack = [(y, start_x)]
            seen[y, start_x] = True
            min_x = max_x = start_x
            min_y = max_y = y
            area = 0

            while stack:
                current_y, current_x = stack.pop()
                area += 1
                min_x = min(min_x, current_x)
                max_x = max(max_x, current_x)
                min_y = min(min_y, current_y)
                max_y = max(max_y, current_y)

                for dy in (-1, 0, 1):
                    for dx in (-1, 0, 1):
                        if dy == 0 and dx == 0:
                            continue
                        next_y = current_y + dy
                        next_x = current_x + dx
                        if (
                            0 <= next_y < height
                            and 0 <= next_x < width
                            and mask[next_y, next_x]
                            and not seen[next_y, next_x]
                        ):
                            seen[next_y, next_x] = True
                            stack.append((next_y, next_x))

            components.append((min_x, min_y, max_x - min_x + 1, max_y - min_y + 1, area))

    return components


def detect_non_food_image_with_pil(image_path):
    try:
        import numpy as np
        from PIL import Image, ImageFilter

        image = Image.open(image_path).convert("RGB")
        image.thumbnail((256, 256))
        rgb = np.array(image)
        height, width = rgb.shape[:2]
        if height < 80 or width < 80:
            return "The image is too small to identify food safely."

        gray = np.array(image.convert("L"))
        edges = np.array(image.convert("L").filter(ImageFilter.FIND_EDGES))
        edge_density = float(np.mean(edges > 50))

        red = rgb[:, :, 0].astype("float32")
        green = rgb[:, :, 1].astype("float32")
        blue = rgb[:, :, 2].astype("float32")
        channel_max = np.max(rgb, axis=2).astype("float32")
        channel_min = np.min(rgb, axis=2).astype("float32")
        saturation_proxy = channel_max - channel_min

        colorfulness = float(
            np.std(red - green)
            + np.std((0.5 * (red + green)) - blue)
        )

        quantized = (rgb // 32).reshape(-1, 3)
        _, counts = np.unique(quantized, axis=0, return_counts=True)
        dominant_ratio = float(counts.max() / counts.sum())

        bright_ratio = float(np.mean(gray > 185))
        dark_ratio = float(np.mean(gray < 35))

        bright_components = _count_mask_components(gray > 185)
        text_components = 0
        text_area = 0
        image_area = max(width * height, 1)
        for _, _, component_width, component_height, area in bright_components:
            if area < 5 or area > image_area * 0.08:
                continue
            aspect = component_width / max(component_height, 1)
            fill_ratio = area / max(component_width * component_height, 1)
            if (
                3 <= component_height <= height * 0.25
                and 0.08 <= fill_ratio <= 0.82
                and 0.12 <= aspect <= 15
            ):
                text_components += 1
                text_area += area

        text_area_ratio = float(text_area / image_area)
        warm_food = (red > green) & (green > blue) & (saturation_proxy > 30)
        green_food = (green > red) & (green > blue) & (saturation_proxy > 30)
        browned_food = (red > 100) & (green > 40) & (blue < 100) & (saturation_proxy > 35)
        food_color_ratio = float(np.mean((warm_food | green_food | browned_food) & (gray > 35)))

        if (
            dominant_ratio > 0.35
            and bright_ratio > 0.32
            and dark_ratio > 0.14
            and food_color_ratio < 0.24
        ):
            return "The image looks like a graphic, poster, or logo rather than a meal."

        if (
            text_components >= 3
            and text_area_ratio > 0.018
            and bright_ratio > 0.40
            and dominant_ratio > 0.30
            and food_color_ratio < 0.24
        ):
            return "The image looks like text or a designed graphic rather than food."

        if food_color_ratio < 0.04 and colorfulness < 25 and edge_density < 0.12 and dominant_ratio > 0.45:
            return "No clear food colors or natural food texture were detected."

        return None
    except Exception:
        return None


def detect_non_food_image(image_path):
    pil_reason = detect_non_food_image_with_pil(image_path)
    if pil_reason:
        return pil_reason

    try:
        import cv2
        import numpy as np
        from PIL import Image

        image = Image.open(image_path).convert("RGB")
        image.thumbnail((512, 512))
        rgb = np.array(image)
        height, width = rgb.shape[:2]
        if height < 80 or width < 80:
            return "The image is too small to identify food safely."

        hsv = cv2.cvtColor(rgb, cv2.COLOR_RGB2HSV)
        saturation_mean = float(np.mean(hsv[:, :, 1]))

        rgb_float = rgb.astype("float32")
        rg = rgb_float[:, :, 0] - rgb_float[:, :, 1]
        yb = 0.5 * (rgb_float[:, :, 0] + rgb_float[:, :, 1]) - rgb_float[:, :, 2]
        colorfulness = float((np.std(rg) ** 2 + np.std(yb) ** 2) ** 0.5 + 0.3 * ((np.mean(rg) ** 2 + np.mean(yb) ** 2) ** 0.5))

        gray = cv2.cvtColor(rgb, cv2.COLOR_RGB2GRAY)
        blur = cv2.GaussianBlur(gray, (3, 3), 0)
        edges = cv2.Canny(blur, 60, 160)
        edge_density = float(np.mean(edges > 0))

        local_mean = cv2.blur(gray.astype("float32"), (13, 13))
        local_sq_mean = cv2.blur((gray.astype("float32") ** 2), (13, 13))
        local_std = np.sqrt(np.maximum(local_sq_mean - local_mean ** 2, 0))
        flat_ratio = float(np.mean(local_std < 8))

        quantized = (rgb // 32).reshape(-1, 3)
        _, counts = np.unique(quantized, axis=0, return_counts=True)
        dominant_ratio = float(counts.max() / counts.sum())

        # Logos, posters, and screenshots often contain many bright text components.
        _, bright_mask = cv2.threshold(gray, 185, 255, cv2.THRESH_BINARY)
        num_labels, labels, stats, _ = cv2.connectedComponentsWithStats(bright_mask, 8)
        text_components = 0
        text_area = 0
        image_area = max(width * height, 1)
        for index in range(1, num_labels):
            x, y, w, h, area = stats[index]
            if area < 8 or area > image_area * 0.08:
                continue
            aspect = w / max(h, 1)
            fill_ratio = area / max(w * h, 1)
            if 4 <= h <= height * 0.22 and 0.08 <= fill_ratio <= 0.75 and 0.15 <= aspect <= 12:
                text_components += 1
                text_area += area

        text_area_ratio = float(text_area / image_area)

        food_like_mask = (
            ((hsv[:, :, 0] < 28) | ((hsv[:, :, 0] > 35) & (hsv[:, :, 0] < 95)))
            & (hsv[:, :, 1] > 35)
            & (hsv[:, :, 2] > 40)
        )
        food_color_ratio = float(np.mean(food_like_mask))

        if (
            text_components >= 18
            and text_area_ratio >= 0.012
            and food_color_ratio < 0.30
            and (flat_ratio > 0.38 or dominant_ratio > 0.35)
        ):
            return "The image looks like text, a logo, or a poster rather than a meal."

        if dominant_ratio > 0.58 and text_components >= 8:
            return "The image looks like a graphic or screenshot rather than a food photo."

        if flat_ratio > 0.72 and colorfulness < 28 and edge_density < 0.08:
            return "The image does not have enough natural food texture."

        if food_color_ratio < 0.055 and saturation_mean < 38:
            return "No clear food colors or ingredients were detected."

        return None
    except Exception:
        return None


def fallback_scan(image_path):
    try:
        from PIL import Image, ImageStat

        image = Image.open(image_path).convert("RGB")
        image.thumbnail((160, 160))
        stat = ImageStat.Stat(image)
        red, green, blue = stat.mean
        brightness = (red + green + blue) / 3
        green_bias = green - max(red, blue)
        warm_bias = red + green - (blue * 1.4)

        name = "Balanced Meal Plate"
        ingredients = ["lean protein", "carbohydrate serving", "vegetables"]
        confidence = 0.52

        if green_bias > 12:
            name = "Fresh Salad Bowl"
            ingredients = ["mixed greens", "vegetables", "light dressing"]
            confidence = 0.58
        elif warm_bias > 120 and brightness > 115:
            name = "Pasta or Rice Bowl"
            ingredients = ["grains", "sauce", "vegetables"]
            confidence = 0.55
        elif brightness < 85:
            name = "Grilled Meal Plate"
            ingredients = ["grilled protein", "side dish", "vegetables"]
            confidence = 0.50
    except Exception:
        name = "Balanced Meal Plate"
        ingredients = ["protein", "carbs", "vegetables"]
        confidence = 0.45

    filename = Path(image_path).stem.lower()
    filename_overrides = {
        "pizza": "Pizza",
        "burger": "Burger",
        "pasta": "Pasta",
        "rice": "Rice Bowl",
        "salad": "Fresh Salad Bowl",
        "chicken": "Chicken Plate",
        "fish": "Fish Plate",
    }
    for token, label in filename_overrides.items():
        if token in filename:
            name = label
            confidence = max(confidence, 0.62)
            break

    nutrition = estimate_nutrition(name, ingredients)
    return {
        "isFood": True,
        "foodName": name,
        "confidence": confidence,
        **nutrition,
        "ingredients": ingredients,
        "instructions": [],
        "source": "image-fallback",
        "note": "Fallback estimate used when the inverse-cooking model is unavailable.",
    }


def patch_torchvision_downloads():
    import torchvision.models as tv_models
    from modules import encoder as encoder_module

    def wrap(model_fn):
        def factory(pretrained=True):
            try:
                return model_fn(weights=None)
            except TypeError:
                return model_fn(pretrained=False)

        return factory

    for name in ("resnet18", "resnet50", "resnet101", "resnet152", "vgg16", "vgg19", "inception_v3"):
        if hasattr(tv_models, name):
            setattr(encoder_module, name, wrap(getattr(tv_models, name)))


def inverse_cooking_scan(image_path):
    data_dir = ROOT / "inversecooking" / "data"
    model_path = data_dir / "modelbest.ckpt"
    ingr_vocab_path = data_dir / "ingr_vocab.pkl"
    instr_vocab_path = data_dir / "instr_vocab.pkl"

    if not model_path.exists() or not ingr_vocab_path.exists() or not instr_vocab_path.exists():
        raise FileNotFoundError("Inverse-cooking model files are missing.")

    inverse_src = ROOT / "inversecooking" / "src"
    sys.path.insert(0, str(inverse_src))

    import pickle
    import torch
    from PIL import Image
    from torchvision import transforms

    from args import get_parser
    from model import get_model
    from utils.output_utils import prepare_output

    patch_torchvision_downloads()

    device = torch.device("cpu")
    ingrs_vocab = pickle.load(open(ingr_vocab_path, "rb"))
    vocab = pickle.load(open(instr_vocab_path, "rb"))

    args = get_parser()
    args.maxseqlen = 15
    args.ingrs_only = False
    args.recipe_only = False

    model = get_model(args, len(ingrs_vocab), len(vocab))
    try:
        state = torch.load(model_path, map_location="cpu", weights_only=False)
    except TypeError:
        state = torch.load(model_path, map_location="cpu")

    if isinstance(state, dict) and "state_dict" in state:
        state = state["state_dict"]

    model.load_state_dict(state, strict=False)
    model.to(device)
    model.eval()
    model.ingrs_only = False
    model.recipe_only = False

    image = Image.open(image_path).convert("RGB")
    transform_image = transforms.Compose([transforms.Resize(256), transforms.CenterCrop(224)])
    transform_tensor = transforms.Compose(
        [
            transforms.ToTensor(),
            transforms.Normalize((0.485, 0.456, 0.406), (0.229, 0.224, 0.225)),
        ]
    )
    image_tensor = transform_tensor(transform_image(image)).unsqueeze(0).to(device)

    with torch.no_grad():
        outputs = model.sample(image_tensor, greedy=True, temperature=1.0, beam=-1, true_ingrs=None)

    ingredient_ids = outputs["ingr_ids"].cpu().numpy()[0]
    recipe_ids = outputs["recipe_ids"].cpu().numpy()[0]
    result, valid = prepare_output(recipe_ids, ingredient_ids, ingrs_vocab, vocab)

    food_name = result.get("title") or "Scanned Meal"
    ingredients = result.get("ingrs") or []
    instructions = result.get("recipe") or []
    nutrition = estimate_nutrition(food_name, ingredients)
    confidence = 0.82 if valid.get("is_valid") else 0.64

    return {
        "isFood": True,
        "foodName": food_name,
        "confidence": confidence,
        **nutrition,
        "ingredients": ingredients[:12],
        "instructions": instructions[:6],
        "source": "inverse-cooking",
        "note": valid.get("reason"),
    }


def scan_food(payload):
    image_path = pick(payload, "imagePath", "ImagePath", "imageUrl", "ImageUrl")
    if not image_path:
        raise ValueError("imagePath is required.")

    image_path = str(image_path)
    non_food_reason = detect_non_food_image(image_path)
    if non_food_reason:
        return non_food_result(non_food_reason)

    try:
        return inverse_cooking_scan(image_path)
    except Exception as exc:
        result = fallback_scan(image_path)
        result["modelError"] = str(exc)
        return result


def main():
    parser = argparse.ArgumentParser(description="Eatopia AI command bridge")
    parser.add_argument("command", choices=["diet-plan", "scan"])
    args = parser.parse_args()

    payload = read_payload()
    if args.command == "diet-plan":
        result = build_diet_plan(payload)
    else:
        result = scan_food(payload)

    print(json.dumps(result, ensure_ascii=False))


if __name__ == "__main__":
    main()
