using UnityEngine;

namespace DeliveryService.Yun.Audio
{
    public enum SoundCategory
    {
        Bgm = 0,
        Sfx = 1,
        Ui = 2,
        Ambience = 3
    }

    public enum SoundSpatialMode
    {
        TwoDimensional = 0,
        ThreeDimensional = 1
    }

    /// <summary>
    /// 사운드 슬롯 식별자. 정수 값은 고정이며, 항목을 추가할 때는 빈 번호를 사용한다.
    /// </summary>
    public enum SoundId
    {
        [InspectorName("플레이어/걷기 발소리")]
        PlayerWalkFootstep = 100,
        [InspectorName("플레이어/달리기 발소리")]
        PlayerRunFootstep = 101,
        [InspectorName("플레이어/점프")]
        PlayerJump = 102,
        [InspectorName("플레이어/착지")]
        PlayerLand = 103,

        [InspectorName("주문/주문서 표시")]
        OrderTicketShow = 200,
        [InspectorName("주문/수락")]
        OrderAccept = 201,
        [InspectorName("주문/거절")]
        OrderReject = 202,
        [InspectorName("배달/성공")]
        DeliverySuccess = 203,
        [InspectorName("배달/거부")]
        DeliveryRefuse = 204,
        [InspectorName("UI/열기")]
        UiOpen = 205,
        [InspectorName("UI/닫기")]
        UiClose = 206,
        [InspectorName("UI/클릭")]
        UiClick = 207,
        [InspectorName("UI/정산 완료")]
        SettlementComplete = 208,

        [InspectorName("피자/재료 집기")]
        IngredientPickup = 300,
        [InspectorName("피자/도우 놓기")]
        DoughPlace = 301,
        [InspectorName("피자/토핑 추가")]
        ToppingAdd = 302,
        [InspectorName("피자/피자 집기")]
        PizzaPickup = 303,
        [InspectorName("피자/피자 내려놓기")]
        PizzaPutDown = 304,
        [InspectorName("피자/오븐에 넣기")]
        OvenInsert = 305,
        [InspectorName("피자/오븐 가동 반복음")]
        OvenLoop = 306,
        [InspectorName("피자/굽기 완료")]
        BakeComplete = 307,
        [InspectorName("피자/오븐에서 꺼내기")]
        OvenRemove = 308,
        [InspectorName("피자/포장")]
        PizzaPack = 309,
        [InspectorName("피자/포장 피자 집기")]
        PackedPizzaPickup = 310,
        [InspectorName("피자/쓰레기 버리기")]
        TrashDiscard = 311,

        [InspectorName("오토바이/탑승")]
        MotorcycleMount = 400,
        [InspectorName("오토바이/하차")]
        MotorcycleDismount = 401,
        [InspectorName("오토바이/시동")]
        MotorcycleEngineStart = 402,
        [InspectorName("오토바이/엔진 반복음")]
        MotorcycleEngineLoop = 403,
        [InspectorName("오토바이/주행 바람 반복음")]
        MotorcycleWindLoop = 404,
        [InspectorName("오토바이/브레이크")]
        MotorcycleBrake = 405,
        [InspectorName("오토바이/장애물 충돌")]
        MotorcycleObstacleHit = 406,
        [InspectorName("오토바이/NPC 충돌")]
        MotorcycleNpcHit = 407,
        [InspectorName("오토바이/고장 반복음")]
        MotorcycleBreakdownLoop = 408,
        [InspectorName("오토바이/엔진 꺼짐")]
        MotorcycleEngineOff = 409,

        [InspectorName("주유/노즐 집기")]
        FuelNozzlePickup = 500,
        [InspectorName("주유/노즐 연결")]
        FuelNozzleConnect = 501,
        [InspectorName("주유/주유 반복음")]
        FuelLoop = 502,
        [InspectorName("주유/노즐 분리")]
        FuelNozzleDisconnect = 503,
        [InspectorName("주유/노즐 반납")]
        FuelNozzleReturn = 504,
        [InspectorName("주유/완료")]
        FuelComplete = 505,

        [InspectorName("도시/NPC 발소리")]
        NpcFootstep = 600,
        [InspectorName("도시/NPC 사고 반응")]
        NpcAccidentReaction = 601,
        [InspectorName("도시/차량 엔진 반복음")]
        VehicleEngineLoop = 602,
        [InspectorName("도시/차량 주행 반복음")]
        VehicleDriveLoop = 603,
        [InspectorName("도시/가게 환경 반복음")]
        ShopAmbienceLoop = 604,
        [InspectorName("도시/낮 환경 반복음")]
        CityDayAmbienceLoop = 605,
        [InspectorName("도시/밤 환경 반복음")]
        CityNightAmbienceLoop = 606,

        [InspectorName("경제/수입")]
        MoneyIncome = 700,
        [InspectorName("경제/지출")]
        MoneyExpense = 701,
        [InspectorName("경제/구매 성공")]
        PurchaseSuccess = 702,
        [InspectorName("경제/구매 실패")]
        PurchaseFail = 703,

        [InspectorName("BGM/기본 게임")]
        GameBgm = 800,

        [InspectorName("예약/온기 부족 경고")]
        WarmthLowWarning = 900,
        [InspectorName("예약/배달 제한시간 임박")]
        DeliveryTimeWarning = 901,
        [InspectorName("예약/영업 시작")]
        BusinessOpen = 902,
        [InspectorName("예약/영업 마감")]
        BusinessClose = 903,
        [InspectorName("예약/다음 날 시작")]
        NextDayStart = 904,
        [InspectorName("예약/월세 납부")]
        RentPayment = 905,
        [InspectorName("예약/파산")]
        Bankruptcy = 906,
        [InspectorName("예약/수리 반복음")]
        RepairLoop = 907,
        [InspectorName("예약/수리 완료")]
        RepairComplete = 908,
        [InspectorName("예약/합의 선택")]
        SettlementChoice = 909,
        [InspectorName("예약/도주 선택")]
        FleeChoice = 910,
        [InspectorName("예약/소매치기 성공")]
        PickpocketSuccess = 911,
        [InspectorName("예약/소매치기 실패")]
        PickpocketFail = 912,
        [InspectorName("예약/경찰 사이렌 반복음")]
        PoliceSirenLoop = 913,
        [InspectorName("예약/수배 시작")]
        WantedStart = 914,
        [InspectorName("예약/추격 해제")]
        ChaseEnd = 915
    }
}
