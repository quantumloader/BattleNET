# Справка по CLI

Проект: `BattleNET.Cli` · пакет: `BattleNET.Cli` · команда инструмента: `battlenet`

## Установка

```bash
dotnet tool install -g BattleNET.Cli
dotnet run --project BattleNET.Cli -- [options]
# или zip BattleNET-*-win-x64.zip из Releases (нужен .NET 9 Runtime)
```

## Параметры

| Параметр | Короткий | Описание |
|----------|----------|----------|
| `--host` | | Хост или IP RCon |
| `--port` | `-p` | UDP-порт RCon |
| `--password` | | Пароль RCon |
| `--command` | `-c` | Одна команда и выход |
| `--reconnect` | | Переподключение при обрыве |
| `--help` | `-?` / `-h` | Справка |

Если host/port/password не указаны — CLI спросит интерактивно (пароль скрыт).

## Примеры

```bash
BattleNET --host 127.0.0.1 --port 2302 --password secret
BattleNET --host 127.0.0.1 --port 2302 --password secret --command "say -1 Привет"
BattleNET --host 127.0.0.1 --port 2302 --password secret --reconnect
```

## Интерактивный режим

| Ввод | Действие |
|------|----------|
| любая RCon-команда | Отправка; ответ печатается |
| `/players` | Разобранный список игроков |
| `/bans` | Разобранный список банов |
| `/history` | История команд сессии |
| `exit` / `logout` / `quit` | Выход |

Ctrl+C отменяет работу и освобождает UDP-порт.

## Коды выхода

| Код | Значение |
|-----|----------|
| `0` | Успех |
| `1` | Нет учётных данных |
| `2` | Ошибка подключения / логина |
| `3` | Ошибка команды (one-shot) |
