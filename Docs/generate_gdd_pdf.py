#!/usr/bin/env python3
"""Generate Echoes of Ascent GDD PDF."""

from pathlib import Path
from fpdf import FPDF

OUTPUT = Path(__file__).parent / "EchoesOfAscent_GDD.pdf"
FONT_REGULAR = Path(r"C:\Windows\Fonts\arial.ttf")
FONT_BOLD = Path(r"C:\Windows\Fonts\arialbd.ttf")
FONT_ITALIC = Path(r"C:\Windows\Fonts\ariali.ttf")


class GDDPdf(FPDF):
    def header(self):
        if self.page_no() == 1:
            return
        self.set_font("ArialI", "", 8)
        self.set_text_color(120, 120, 120)
        self.cell(0, 8, "Echoes of Ascent — Game Design Document v1.0", align="C")
        self.ln(10)

    def footer(self):
        self.set_y(-15)
        self.set_font("ArialI", "", 8)
        self.set_text_color(120, 120, 120)
        self.cell(0, 10, f"Стр. {self.page_no()}", align="C")

    def section_title(self, title: str):
        self.ln(4)
        self.set_font("ArialB", "", 14)
        self.set_text_color(30, 30, 30)
        self.multi_cell(0, 8, title)
        self.ln(2)

    def subsection_title(self, title: str):
        self.ln(2)
        self.set_font("ArialB", "", 11)
        self.set_text_color(50, 50, 50)
        self.multi_cell(0, 7, title)
        self.ln(1)

    def body_text(self, text: str):
        self.set_font("Arial", "", 10)
        self.set_text_color(20, 20, 20)
        self.multi_cell(0, 5.5, text)
        self.ln(1)

    def bullet(self, text: str):
        self.set_x(self.l_margin)
        self.set_font("Arial", "", 10)
        self.set_text_color(20, 20, 20)
        self.multi_cell(0, 5.5, f"- {text}")

    def _cell_line_count(self, text: str, width: float, line_h: float) -> int:
        lines = self.multi_cell(width, line_h, str(text), dry_run=True, output="LINES")
        return max(1, len(lines))

    def table(self, headers, rows, col_widths=None, bold_header=True):
        """Render a full table with proper row heights and borders."""
        self.set_x(self.l_margin)
        epw = self.epw
        n = len(headers)

        if col_widths is None:
            col_widths = [epw / n] * n
        else:
            total = sum(col_widths)
            col_widths = [w / total * epw for w in col_widths]

        line_h = 5.0
        pad = 1.5

        def draw_row(cells, header=False):
            self.set_font("ArialB" if header else "Arial", "", 9)
            self.set_text_color(20, 20, 20)

            heights = [
                self._cell_line_count(cell, w - 2 * pad, line_h) * line_h
                for cell, w in zip(cells, col_widths)
            ]
            row_h = max(heights)

            if self.get_y() + row_h > self.page_break_trigger:
                self.add_page()
                self.set_x(self.l_margin)

            x0 = self.l_margin
            y0 = self.get_y()

            for i, (cell, w) in enumerate(zip(cells, col_widths)):
                x = x0 + sum(col_widths[:i])
                self.rect(x, y0, w, row_h)
                self.set_xy(x + pad, y0 + pad)
                self.multi_cell(w - 2 * pad, line_h, str(cell))

            self.set_xy(x0, y0 + row_h)

        if bold_header:
            draw_row(headers, header=True)
        else:
            draw_row(headers)

        for row in rows:
            draw_row(row)

        self.ln(2)


def build_pdf() -> Path:
    pdf = GDDPdf()
    pdf.set_auto_page_break(auto=True, margin=20)
    pdf.add_font("Arial", "", str(FONT_REGULAR))
    pdf.add_font("ArialB", "", str(FONT_BOLD))
    pdf.add_font("ArialI", "", str(FONT_ITALIC))
    pdf.add_page()

    # Title page
    pdf.ln(40)
    pdf.set_font("ArialB", "", 28)
    pdf.set_text_color(30, 30, 30)
    pdf.cell(0, 14, "Echoes of Ascent", align="C", new_x="LMARGIN", new_y="NEXT")
    pdf.ln(4)
    pdf.set_font("Arial", "", 16)
    pdf.set_text_color(80, 80, 80)
    pdf.cell(0, 10, "Game Design Document", align="C", new_x="LMARGIN", new_y="NEXT")
    pdf.ln(8)
    pdf.set_font("ArialI", "", 11)
    pdf.multi_cell(0, 7, "Вертикальный путь наверх, где каждое движение имеет вес,\nа свет прокладывает дорогу.", align="C")
    pdf.ln(20)
    pdf.set_font("Arial", "", 10)
    pdf.set_text_color(100, 100, 100)
    pdf.cell(0, 6, "Версия: 1.0  |  Дата: 13.07.2026  |  Платформа: iOS / Android", align="C", new_x="LMARGIN", new_y="NEXT")
    pdf.cell(0, 6, "Движок: Unity (URP)  |  Статус: Прототип с рабочим core loop", align="C")

    pdf.add_page()

    # 1. Vision
    pdf.section_title("1. Видение")
    pdf.subsection_title("1.1 Elevator Pitch")
    pdf.body_text(
        "Вы — бестелесный дух, поднимающийся по руинам древней башни наверх, чтобы вернуть миру солнечный свет. "
        "Каждый прыжок рассчитывается свайпом; каждая платформа открывает новый визуальный слой. "
        "Игра сочетает точную физику платформера с медитативной эстетикой «тихой роскоши» — без видимых кнопок и визуального шума."
    )
    pdf.subsection_title("1.2 Теглайн")
    pdf.body_text("«Вертикальный путь наверх, где каждое движение имеет вес, а свет прокладывает дорогу.»")
    pdf.subsection_title("1.3 Целевая фантазия (Hook)")
    for item in [
        "Состояние потока от идеального прыжка",
        "Спокойствие в минималистичном пространстве с earth tones",
        "Эстетическое удовлетворение от постепенного «просветления» мира",
        "Тихое соревнование — следы падений других игроков как мягкие напоминания",
    ]:
        pdf.bullet(item)
    pdf.subsection_title("1.4 USP (Уникальное торговое предложение)")
    for item in [
        "Геометрически точные прыжки (свайп → траектория → физика)",
        "Расслабляющая эстетика natural textures без ярких вспышек",
        "Полностью скрытый UI — навигация только жестами",
        "Социальный слой «эхо» — свечения на местах чужих падений",
    ]:
        pdf.bullet(item)

    # 2. Audience
    pdf.section_title("2. Целевая аудитория")
    pdf.table(
        ["Параметр", "Описание"],
        [
            ("Платформа", "Мобильные игроки iOS / Android"),
            ("Время сессии", "15–20 минут"),
            ("Мотивация", "Не тайм-киллер, а эстетичный опыт"),
            ("Вкусы", "Monument Valley, Alto's Adventure, Gris"),
            ("UI-ожидания", "Минимализм, без баннеров и перегруженных HUD"),
            ("Навык", "Средний — вызов есть, но без Jump King-фрустрации"),
        ],
        [1, 3],
    )

    # 3. References
    pdf.section_title("3. Референсы")
    pdf.table(
        ["Игра", "Берём", "Не берём"],
        [
            ("Gris", "Цвет, мягкие тона, медитативность", "Линейный нарратив, PC-формат"),
            ("Jump King", "Расчёт силы прыжка, вертикальный подъём", "Падение в самый низ, агрессивная сложность"),
            ("Alto's Adventure", "Flow, минимализм, плавность", "Бесконечный раннер"),
            ("Monument Valley", "Чистый UI, геометрическая эстетика", "Головоломки перспективы"),
        ],
        [1, 2, 2],
    )

    # 4. Core Loop
    pdf.section_title("4. Core Gameplay Loop")
    pdf.body_text(
        "Оценить дистанцию → Натянуть свайп (превью траектории) → Отпустить → прыжок → "
        "Приземление на платформу → чекпоинт → Плавный переход визуального слоя → Повтор до вершины"
    )
    pdf.subsection_title("4.1 Управление")
    pdf.table(
        ["Действие", "Ввод", "Реализация"],
        [
            ("Прицеливание", "Удержание свайпа", "SwipeInputProvider → OnSwipeUpdated"),
            ("Прыжок", "Отпускание свайпа", "PlayerSpiritController.HandleSwipeReleased"),
            ("Отмена", "Короткий свайп (<25px)", "OnSwipeCancelled"),
        ],
        [1, 1.2, 2.2],
    )
    pdf.subsection_title("Параметры прыжка (JumpConfig)")
    for item in [
        "Импульс: 4–18 (по длине свайпа)",
        "Угол: 8°–75° от горизонтали (только вверх)",
        "Coyote time: 0.12 с",
        "Превью: до 40 точек траектории",
    ]:
        pdf.bullet(item)
    pdf.subsection_title("4.2 Физика и ощущения")
    for item in [
        "Rigidbody + гравитация Unity",
        "Направление свайпа → мировая скорость через камеру",
        "Поддержка движущихся платформ (CircularArcMover)",
        "Падение с платформы → мягкий респавн на последнем чекпоинте",
    ]:
        pdf.bullet(item)
    pdf.subsection_title("4.3 Прогрессия уровня")
    for item in [
        "11 платформ на уровень (ProceduralLevelGenerator)",
        "Каждая платформа = чекпоинт",
        "Финальная платформа → событие LevelCompleted",
        "Генерация гарантирует проходимость (JumpReachAnalyzer)",
    ]:
        pdf.bullet(item)

    pdf.add_page()

    # 5. Level structure
    pdf.section_title("5. Уровневая структура")
    pdf.subsection_title("5.1 Текущая модель (реализовано)")
    for item in [
        "Процедурная генерация по shared seed",
        "Seed общий для всех устройств через backend (/api/session)",
        "Layout: вертикальный зигзаг с учётом досягаемости прыжка",
        "Платформы: процедурные каменные меши из каталога префабов",
        "Ширина: 2.2–3.2, подъём: 0.9–3.0 между платформами",
    ]:
        pdf.bullet(item)
    pdf.subsection_title("5.2 Целевая модель (к доработке)")
    for item in [
        "Статичные экраны-головоломки с авторским дизайном",
        "Ручная кривая сложности по главам",
        "Нарративные «зоны» башни",
    ]:
        pdf.bullet(item)
    pdf.subsection_title("5.3 Чекпоинты и сохранение")
    pdf.table(
        ["Данные", "Хранение", "Ключ"],
        [
            ("Последний чекпоинт", "PlayerPrefs", "echoes_checkpoint_id"),
            ("Seed уровня", "PlayerPrefs", "echoes_level_seed"),
        ],
        [1.2, 1, 1.5],
    )

    # 6. Visual design
    pdf.section_title("6. Визуальный дизайн")
    pdf.subsection_title("6.1 Арт-направление")
    for item in [
        "Палитра: earth tones → постепенное осветление к вершине",
        "Текстуры: natural stone, процедурные шейдеры",
        "Освещение: мягкий directional light, экспоненциальный fog",
        "Пост-обработка: Color Adjustments, Bloom, Vignette (URP Volume)",
    ]:
        pdf.bullet(item)
    pdf.subsection_title("6.2 Визуальные слои")
    pdf.table(
        ["Слой", "Checkpoint", "Fog", "Ambient", "Настроение"],
        [
            ("0", "Старт", "#948066", "#B8A38A", "Тёплая тьма, низ башни"),
            ("1", "~3", "#8A8070", "#B2A894", "Умеренное тепло"),
            ("2", "~6", "#9EA399", "#C7C2B3", "Переход к свету"),
            ("3", "~10", "#BDC7D1", "#E0E6ED", "Холодный рассвет, вершина"),
        ],
        [0.6, 0.8, 0.8, 0.8, 1.5],
    )
    pdf.body_text("Переход: плавный blend 1.5 с (VisualLayerController)")
    pdf.subsection_title("6.3 UI-философия")
    for item in [
        "В геймплее: ноль видимых элементов",
        "Превью траектории: тонкая линия earth tone (#D9BF8C)",
        "Победа: затемнение + «Вы прошли уровень!» (LevelCompleteUI)",
        "Будущее: без меню, настроек, рекламы на экране",
    ]:
        pdf.bullet(item)

    # 7. Audio
    pdf.section_title("7. Аудио (план)")
    pdf.table(
        ["Событие", "Звук", "Характер"],
        [
            ("Свайп (натяжение)", "Тихий тон, нарастающий", "Медитативный"),
            ("Прыжок", "Мягкий whoosh", "Без резкости"),
            ("Приземление", "Приглушённый thud", "Natural"),
            ("Чекпоинт / слой", "Ambient swell", "2–3 с"),
            ("Падение", "Тихий dissolve", "Без наказания звуком"),
            ("Победа", "Открытие, свет", "Эмоциональный пик"),
        ],
        [1.2, 1.3, 1.2],
    )
    pdf.body_text("Аудио в проекте пока не реализовано.")

    # 8. Echoes
    pdf.section_title("8. Meta-система: Echoes")
    pdf.body_text(
        "Уникальная фича проекта. Каждое падение оставляет мягкое свечение (echo) на месте последнего "
        "контакта с землёй. Эхо видны всем игрокам с тем же seed уровня."
    )
    pdf.subsection_title("Техническая реализация")
    pdf.body_text(
        "Падение → FallEchoTracker → EchoesManager.RecordFall() → POST /api/fall → локальный EchoGlow prefab.\n"
        "Poll каждые 2 с → GET /api/echoes?seed=N → спавн чужих эхо."
    )
    pdf.subsection_title("Дизайн-цели")
    for item in [
        "Ненавязчивый социальный слой",
        "«Карта опасности» без текста",
        "Усиление атмосферы одиночества / общности",
    ]:
        pdf.bullet(item)

    pdf.add_page()

    # 9. Narrative
    pdf.section_title("9. Нарратив (план)")
    pdf.body_text(
        "Сеттинг: древняя башня-механизм, поглощённая тьмой. Дух восходит по руинам, "
        "чтобы активировать механизм солнечного света."
    )
    pdf.table(
        ["Элемент", "Способ подачи"],
        [
            ("Подъём", "Визуальные слои светлеют"),
            ("Руины", "Каменные процедурные платформы"),
            ("Дух", "Форма игрока, эхо падений"),
            ("Цель", "Финальная платформа, победа"),
            ("Лор", "Опционально: короткие тексты при слоях"),
        ],
        [1, 3],
    )
    pdf.body_text("Нарратив сейчас только в концепции — в билде не подан.")

    # 10. Tech architecture
    pdf.section_title("10. Техническая архитектура")
    pdf.body_text(
        "Assets/_Project/: Core (GameEvents, SwipeData), Input (SwipeInputProvider), "
        "Gameplay (прыжок, уровень, чекпоинты, эхо), Presentation (камера, визуальные слои, UI), "
        "Meta (SaveService, EchoApi). Backend: Express API (seed, falls, echoes)."
    )
    pdf.table(
        ["Система", "Класс", "Статус"],
        [
            ("Ввод", "SwipeInputProvider", "OK"),
            ("Прыжок", "JumpCalculator, PlayerSpiritController", "OK"),
            ("Уровень", "ProceduralLevelGenerator", "OK"),
            ("Визуал", "VisualLayerController", "OK"),
            ("Респавн", "PlayerRespawnController", "OK"),
            ("Эхо", "EchoesManager, EchoApi", "OK"),
            ("Камера", "CameraFollowController", "OK"),
            ("Победа", "LevelCompleteUI", "OK"),
            ("Движ. платформы", "CircularArcMover", "OK (не в процедурном уровне)"),
            ("Тесты", "EditMode tests", "OK"),
        ],
        [1, 2, 0.7],
    )

    # 11. Balance
    pdf.section_title("11. Баланс и сложность")
    pdf.subsection_title("11.1 Философия")
    for item in [
        "Вызов без фрустрации: падение = респавн на чекпоинте",
        "Читаемость: следующая платформа всегда видна на экране",
        "Комфортный диапазон: импульс 45–82% от максимума при генерации",
    ]:
        pdf.bullet(item)
    pdf.subsection_title("11.2 Кривая сложности")
    pdf.table(
        ["Платформа", "Сложность", "Визуальный слой"],
        [
            ("0–2", "Обучение механике", "Слой 0"),
            ("3–5", "Горизонтальные смещения", "Слой 1"),
            ("6–8", "Больший подъём", "Слой 2"),
            ("9–10", "Финальный рывок", "Слой 3"),
        ],
        [0.8, 1.5, 1.2],
    )

    # 12. Monetization
    pdf.section_title("12. Монетизация (не определена)")
    pdf.body_text(
        "Рекомендация: Premium или одноразовая покупка — соответствует анти-баннерной философии. "
        "Без рекламы, без pay-to-win. Опционально: косметические палитры слоёв."
    )

    # 13. Roadmap
    pdf.section_title("13. Roadmap")
    pdf.subsection_title("Фаза 1 — Core (текущее)")
    for item in [
        "Свайп-прыжок с превью траектории",
        "Процедурная генерация уровня",
        "Чекпоинты и респавн",
        "4 визуальных слоя",
        "Echoes backend",
        "Android build",
        "Unit-тесты прыжка и генерации",
    ]:
        pdf.bullet(f"[x] {item}")
    pdf.subsection_title("Фаза 2 — Polish")
    for item in [
        "Аудио (ambient + SFX)",
        "Параллакс-фоны",
        "Туториал без текста",
        "Движущиеся платформы в процедурной генерации",
        "Haptic feedback",
    ]:
        pdf.bullet(f"[ ] {item}")
    pdf.subsection_title("Фаза 3 — Content")
    for item in [
        "Авторские уровни-головоломки",
        "Нарративные вставки при смене слоёв",
        "Несколько глав башни",
        "Daily seed / shared challenge",
    ]:
        pdf.bullet(f"[ ] {item}")
    pdf.subsection_title("Фаза 4 — Release")
    for item in [
        "iOS build",
        "Production backend",
        "App Store / Google Play",
        "Локализация (RU/EN)",
    ]:
        pdf.bullet(f"[ ] {item}")

    pdf.add_page()

    # 14. Risks
    pdf.section_title("14. Риски")
    pdf.table(
        ["Риск", "Митигация"],
        [
            ("Процедурные уровни скучнее ручных", "Гибрид: curated seeds + авторские финалы"),
            ("Echoes требуют сервер", "Graceful fallback (локальные эхо) уже есть"),
            ("Сложность для casual", "Coyote time, мягкий респавн, видимая траектория"),
            ("Нет нарратива", "Визуальный storytelling через слои"),
            ("LAN backend", "Cloud deploy перед релизом"),
        ],
        [1.2, 2],
    )

    # 15. KPI
    pdf.section_title("15. Метрики успеха (KPI)")
    pdf.table(
        ["Метрика", "Цель"],
        [
            ("Retention D1", "> 35%"),
            ("Средняя сессия", "12–18 мин"),
            ("Completion rate (уровень)", "> 25%"),
            ("Падений на прохождение", "5–15"),
            ("Crash-free sessions", "> 99.5%"),
        ],
        [1.2, 1.5],
    )

    # 16. Comparison
    pdf.section_title("16. Сравнение One-pager и проекта")
    pdf.table(
        ["#", "One-pager", "Проект", "Рекомендация"],
        [
            ("1", "Статичные головоломки", "Процедурная генерация", "Процедурка для MVP"),
            ("2", "Падение в самый низ", "Респавн на чекпоинте", "Сохранить мягкий вариант"),
            ("3", "Нарратив башни", "Не подан", "Storytelling в Фазе 3"),
            ("4", "—", "Echoes (соц. слой)", "Включить как USP"),
            ("5", "Ноль UI", "Victory screen", "Заменить анимацией"),
        ],
        [0.4, 1.2, 1.2, 1.2],
    )

    pdf.ln(8)
    pdf.set_font("ArialI", "", 9)
    pdf.set_text_color(100, 100, 100)
    pdf.multi_cell(
        0, 5,
        "Документ синтезирован из One-pager (Echoes of Ascent) и фактического состояния кодовой базы Unity-проекта.",
    )

    pdf.output(str(OUTPUT))
    return OUTPUT


if __name__ == "__main__":
    path = build_pdf()
    print(f"Saved: {path}")
