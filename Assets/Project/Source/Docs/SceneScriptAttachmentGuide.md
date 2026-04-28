# Куда вешать скрипты

## Player

На корневой объект игрока:

- `CharacterController` - добавляется вручную в инспекторе, скрипты его не создают.
- `DesktopFirstPersonController` - движение, камера, ввод, удержание кнопки взаимодействия.
- `PlayerInteractionRaycaster` - луч из камеры, поиск объектов на слоях `Interactable` и `Outline`.

В `DesktopFirstPersonController` укажи `Camera Transform`, если он не находится автоматически.
В `PlayerInteractionRaycaster` укажи `Player Input`, `Ray Camera`, `Interaction Text Config`.

## Scripts

На отдельный сценовый объект `Scripts`:

- `BrakeDiscScenarioManager` - порядок сценария, текущая цель, HUD, проверка инструментов.
- `MechanicToolController` - инструменты на клавишах `1-9`, сброс инструмента на `F`.

В `BrakeDiscScenarioManager` заполни:

- `Hud` - объект с `MechanicPlayerHud`.
- `Text Config` - `Assets/Project/Source/Configs/BrakeDiscScenarioTextConfig.asset`.
- `Tool Controller` - `MechanicToolController` с объекта `Scripts`.
- `Actions` - все объекты со `ScenarioStepInteractable` в порядке не обязателен, главное чтобы все были в списке.

В `MechanicToolController` заполни `Tools`:

- `Tool Id`: `DynamometricWrench`.
- `Display Name`: `Динамометрический ключ`.
- `Hand Object`: 3D-модель инструмента в руке игрока.

## HUD

На объект UI или на объект игрока, где удобнее:

- `MechanicPlayerHud` - приветственное окно, текущая цель, прогресс, подсказка взаимодействия, текущий инструмент снизу по центру.

В `MechanicPlayerHud` укажи:

- `Interaction Raycaster` - `PlayerInteractionRaycaster` игрока.
- `Text Config` - `Assets/Project/Source/Configs/MechanicHudTextConfig.asset`.

## Каждый интерактивный объект сценария

На каждый объект, по которому игрок должен кликнуть:

- `Collider` - чтобы луч мог попасть в объект.
- слой `Interactable`.
- `ScenarioStepInteractable`.
- `SelectionOutline`.

В `ScenarioStepInteractable` укажи:

- `Step` - шаг сценария, которому принадлежит объект.
- `Requires Hold` - включить для действий с удержанием.
- `Hold Duration` - длительность удержания.
- `Enable On Complete` - что включить после клика.
- `Disable On Complete` - что выключить после клика.
- `Disable This Object On Complete` - если сам объект больше не нужен.

`Required Tool Id` обычно оставляй пустым. Тогда инструмент берётся из `BrakeDiscScenarioTextConfig.asset`. Заполняй это поле только если конкретному объекту нужен нестандартный инструмент.

`Hide Renderers Until Focused` используй только если хочешь временно показать мэш при наведении. Для подсказки “виден только Outline, мэш скрыт” лучше используй `InvisibleInteractableMaterial.mat`.

## Как настраивать типовые действия

Снять деталь с машины:

- `ScenarioStepInteractable` ставится на деталь или её collider.
- В `Disable On Complete` добавь видимую деталь на машине.
- Если нужно, в `Enable On Complete` добавь следующий слот на столе.

Положить деталь на стол:

- `ScenarioStepInteractable` ставится на слот стола.
- В `Enable On Complete` добавь видимую деталь на столе.
- Слот держи на слое `Interactable`, а визуальную деталь на столе изначально выключенной.
- Если слот должен быть невидимым, но с Outline, оставь `Renderer` включённым и поставь ему материал `Assets/Project/Source/Shaders/InvisibleInteractableMaterial.mat`.

Взять деталь со стола:

- `ScenarioStepInteractable` ставится на деталь или collider на столе.
- В `Disable On Complete` добавь видимую деталь на столе.
- В `Enable On Complete` добавь следующий слот установки на машине.

Поставить деталь обратно:

- `ScenarioStepInteractable` ставится на слот установки на машине.
- В `Enable On Complete` добавь видимую деталь на машине.
- В `Disable On Complete` добавь слот, если он не должен больше подсвечиваться.
- Для невидимого слота установки тоже используй `InvisibleInteractableMaterial.mat`, а не выключенный `Renderer`.

## Что больше не нужно вешать

`WheelBoltInteractable` и `RemovableWheelInteractable` оставлены как совместимость со старой настройкой, но для новых объектов лучше использовать только `ScenarioStepInteractable`.

`SimplePlayerInteractable` нужен только для простых объектов вне сценария. Для замены тормозного диска он не нужен.
