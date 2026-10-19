using System.Collections.Generic;using System.Linq;using UnityEngine;
namespace DeliveryService.Mok {
public static class PizzaRecipes {
    static readonly string[] Korean={"도우","소스","치즈","고추","페페로니","초콜릿","젤리","파인애플"};
    static readonly ToppingType[] Menus={ToppingType.Cheese,ToppingType.Pepperoni,ToppingType.Pepper,ToppingType.Chocolate,ToppingType.Jelly,ToppingType.Pineapple};
    public static bool IsBase(ToppingType t)=>t==ToppingType.Dough||t==ToppingType.Sauce||t==ToppingType.Cheese;
    public static bool TryType(string value,out ToppingType result){string text=(value??"").Trim().ToLowerInvariant();foreach(ToppingType t in System.Enum.GetValues(typeof(ToppingType)).Cast<ToppingType>().OrderByDescending(t=>t.ToString().Length))if(text.Contains(t.ToString().ToLowerInvariant())||text.Contains(Korean[(int)t])){result=t;return true;}result=default;return false;}
    public static HashSet<ToppingType> Types(IEnumerable<string> values){var set=new HashSet<ToppingType>();if(values!=null)foreach(var v in values)if(TryType(v,out var type))set.Add(type);return set;}
    public static bool Matches(List<string> actual,List<string> requested){if(actual==null||requested==null||actual.Any(v=>!TryType(v,out _)))return false;var a=Types(actual);return a.Contains(ToppingType.Dough)&&a.Contains(ToppingType.Sauce)&&a.Contains(ToppingType.Cheese)&&a.SetEquals(Types(requested));}
    public static PizzaOrder Create(){var menu=Menus[Random.Range(0,Menus.Length)];var values=new List<string>{"도우","소스","치즈"};if(menu!=ToppingType.Cheese)values.Add(Korean[(int)menu]);return new PizzaOrder{recipeName=Korean[(int)menu]+"피자",toppings=values};}
    public static string Title(PizzaOrder order)=>!string.IsNullOrEmpty(order?.recipeName)?order.recipeName:"치즈피자";
}
public sealed class DeliveryRequestNotice : MonoBehaviour {
    string message;float until;
    public static void Show(string value){var n=FindFirstObjectByType<DeliveryRequestNotice>();if(n==null)n=new GameObject("Delivery request notice").AddComponent<DeliveryRequestNotice>();n.message=value;n.until=Time.unscaledTime+3;}
    void OnGUI(){if(Time.unscaledTime>=until)return;var style=new GUIStyle(GUI.skin.box){font=MokUIFont.Font,fontSize=Mathf.RoundToInt(Screen.height*.024f),alignment=TextAnchor.MiddleCenter};GUI.Box(new Rect(Screen.width*.25f,Screen.height*.76f,Screen.width*.5f,Screen.height*.10f),message,style);}
}
}
