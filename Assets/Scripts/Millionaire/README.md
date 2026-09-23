# Millionaire Quiz — настройка

## Что уже создано
- `Assets/Resources/Questions/q0.json … q5.json` — вопросы (можно добавить свои `qN.json` по такому же формату).
- `QuestionData.cs` — модель вопроса.
- `QuestionLoader.cs` — читает все json-файлы из `Resources/Questions` и выдаёт случайные вопросы без повторов.
- `AnswerButtonUI.cs` — компонент для префаба кнопки-ответа.
- `QuizGameManager.cs` — управляет игрой целиком.

## Формат JSON
```json
{
    "question": "Текст вопроса?",
    "answer1": "Вариант 1",
    "answer2": "Вариант 2",
    "answer3": "Вариант 3",
    "answer4": "Вариант 4",
    "correctAnswer": 2
}
```
`correctAnswer` — номер правильного варианта, от 1 до 4. Чтобы добавить вопрос, скопируйте любой файл, переименуйте в `q6.json`, `q7.json` и т.д. (Unity подхватит любое имя внутри папки `Questions`, но их нужно класть именно туда).

## Настройка сцены

1. **Префаб кнопки ответа**
   - Создайте UI Button (`GameObject → UI → Button - TextMeshPro`).
   - На дочернем тексте убедитесь, что используется `TextMeshProUGUI`.
   - Добавьте на корневой объект кнопки компонент `AnswerButtonUI`.
   - В инспекторе `AnswerButtonUI` перетащите:
     - `Label` → дочерний `TextMeshProUGUI` кнопки.
     - `Background` → `Image` кнопки (обычно сам компонент Image на кнопке).
   - При желании настройте `Default / Correct / Wrong Color`.
   - Перетащите кнопку из сцены в `Assets` — получится префаб. Удалите инстанс со сцены.

2. **Контейнер для кнопок**
   - Создайте пустой `GameObject` (например, `AnswersGrid`) внутри Canvas.
   - Добавьте компонент `Grid Layout Group` и настройте `Cell Size`, `Spacing`, `Constraint` (обычно `Fixed Column Count = 2` для сетки 2×2).

3. **Текст вопроса**
   - Создайте `TextMeshProUGUI` на канвасе для отображения вопроса.

4. **(опционально) Экран завершения игры**
   - Создайте панель `GameOverPanel` с `TextMeshProUGUI` для результата и, например, кнопкой "Играть снова".
   - Кнопку "Играть снова" свяжите с методом `QuizGameManager.RestartGame()`.

5. **Game Manager**
   - Создайте пустой `GameObject` (например, `GameManager`) и добавьте компонент `QuizGameManager`.
   - В инспекторе заполните поля:
     - `Question Text` → TMP-текст вопроса из шага 3.
     - `Answer Grid` → `RectTransform` контейнера из шага 2.
     - `Answer Button Prefab` → префаб из шага 1.
     - `Game Over Panel` / `Result Text` → объекты из шага 4 (необязательно).
     - `Feedback Delay` → пауза перед следующим вопросом после ответа (по умолчанию 1.2 сек).

6. Нажмите **Play**. `QuizGameManager` сам загрузит случайный вопрос, заполнит текст и создаст 4 кнопки в `GridLayoutGroup` с вариантами ответов. При неправильном ответе игра завершается и показывает `GameOverPanel` (если он назначен); при ответе на все вопросы — экран победы.

## Логика игры вкратце
- `QuestionLoader` при старте загружает **все** файлы из `Resources/Questions` и на каждый вопрос выдаёт случайный, ещё не заданный вариант (без повторов за игру).
- `QuizGameManager.LoadNextQuestion()` очищает `AnswerGrid`, создаёт 4 кнопки через `Instantiate(answerButtonPrefab, answerGrid)` и вызывает `AnswerButtonUI.Setup(...)` с текстом и индексом ответа.
- При клике по кнопке `QuizGameManager.OnAnswerSelected(index)` блокирует ввод, подсвечивает правильный/неправильный вариант и через `feedbackDelay` либо грузит следующий вопрос, либо завершает игру.
