import type { Messages } from './en'

/** ru needs three plural forms: 1 событие, 2 события, 5 событий. */
function plural(count: number, one: string, few: string, many: string): string {
  const mod10 = count % 10
  const mod100 = count % 100
  if (mod10 === 1 && mod100 !== 11) return one
  if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) return few
  return many
}

export const ru: Messages = {
  documentTitle: 'DotNetScope — панель разработчика .NET',
  app: { name: 'DotNetScope', tagline: 'панель разработчика' },

  nav: {
    all: 'Все события',
    http: 'Запросы',
    sql: 'SQL',
    log: 'Логи',
    exception: 'Исключения',
    database: 'База данных',
  },

  types: { http: 'HTTP', sql: 'SQL', log: 'ЛОГ', exception: 'ОШБ' },

  connection: {
    connecting: 'Подключение…',
    live: 'Онлайн',
    reconnecting: 'Переподключение…',
    offline: 'Нет связи',
    paused: 'Пауза',
  },

  language: { label: 'Язык' },
  privacy: 'Всё хранится в памяти и не покидает процесс.',

  toolbar: {
    filter: 'Фильтр по пути, SQL, сообщению…',
    anyDuration: 'Любая длительность',
    over100: '> 100 мс',
    over500: '> 500 мс',
    over1s: '> 1 с',
    sortLabel: 'Сортировка',
    sortNewest: 'Сначала новые',
    sortSlowest: 'Сначала медленные',
    saveFilter: 'Сохранить фильтр',
    nameFilter: 'Название фильтра',
    save: 'Сохранить',
    saved: 'Сохранённые',
    pause: 'Пауза',
    resume: 'Продолжить',
    clear: 'Очистить',
    applyFilter: 'Применить фильтр',
    deleteFilter: 'Удалить фильтр',
    events: (count: number) => `${count} ${plural(count, 'событие', 'события', 'событий')}`,
  },

  list: {
    time: 'Время',
    type: 'Тип',
    summary: 'Событие',
    status: 'Статус',
    duration: 'Длительность',
    emptyTitle: 'Пока ничего не поймано',
    emptyBody: 'Дёрните свой API — запрос, его SQL и логи появятся здесь в реальном времени.',
    terminalHint: '# в другом терминале',
    noMatches: 'Ни одно событие не подходит под фильтр.',
    noMatchesHint: 'Очистите строку поиска или ослабьте фильтр по длительности.',
  },

  details: {
    placeholderTitle: 'Выберите событие, чтобы посмотреть детали.',
    placeholderBody: 'SQL, тела и стектрейсы сгруппированы по запросу.',
    close: 'Закрыть',
    sql: 'SQL',
    parameters: 'Параметры',
    requestBody: 'Тело запроса',
    responseBody: 'Тело ответа',
    responseBodyJson: 'Тело ответа · JSON',
    exception: 'Исключение',
    related: 'Связанные события',
    noRelated: 'Во время этого запроса не было SQL или логов.',
    partOfRequest: 'Часть запроса',
    category: 'Категория',
    eventId: 'ID события',
    copy: 'копировать',
    copied: 'скопировано',
    copyFrame: 'Скопировать кадр',
    exportMarkdown: 'Копировать markdown',
    exportJson: 'Копировать JSON',
    events: (count: number) => `${count} ${plural(count, 'событие', 'события', 'событий')}`,
  },

  markdown: {
    requestBody: 'Тело запроса',
    responseBody: 'Тело ответа',
    sql: 'SQL',
    logs: 'Логи',
    exception: 'Исключение',
  },

  db: {
    title: 'База данных',
    tables: 'Таблицы',
    subtitle: 'Только чтение, только первые строки',
    noSourcesTitle: 'Нет подключённых баз',
    noSourcesBody: 'Зарегистрируйте подключение там, где настраивается дашборд:',
    noSourcesCode: 'options.AddDataSource("app", () => new SqliteConnection(...))',
    selectTable: 'Выберите таблицу слева.',
    loading: 'Загрузка…',
    refresh: 'Обновить',
    limit: 'Строк',
    rows: (count: number) => `${count} ${plural(count, 'строка', 'строки', 'строк')}`,
    truncated: 'достигнут лимит',
    emptyTable: 'В таблице нет строк.',
    errorTitle: 'Не удалось прочитать таблицу',
    nullValue: 'NULL',
  },
}
