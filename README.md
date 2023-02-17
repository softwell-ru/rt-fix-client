# rt-fix-client

## Конфигурация

В файле [SessionSettings.cfg](./SessionSettings.cfg) шаблон конфигурации сессии FIX. Для указания адреса сервера, логина, пароля и тд, надо в этом файле заменить все строки вида %ИмяПараметра% на реальные значения. Либо создать рядом SessionSettings.local.cfg, скопировать исходный файл туда, и заменить строки в нем.

## Запуск

Заполнить [SessionSettings.cfg](./SessionSettings.cfg), или создать и заполнить SessionSettings.local.cfg

``` console
dotnet build
cd src/ConsoleHost/bin/Debug/net6.0/
dotnet SoftWell.RtFix.ConsoleHost.dll [arguments]
```

либо 

``` console
dotnet build
cd src/ConsoleHost
dotnet run [arguments]
```

где в качестве аргументов можно передать имена сценариев для выполнения.

Если в аргументах будет пусто, то запустятся все сценарии по очереди.

## Сценарии

#### ReceiveDeal

Получить одну сделку в фоновом режиме

#### SendDealsRequestAndReceiveDeals

Отправить запрос на сделки за вчера и сегодня, получить подтверждение запроса, получить сделки

#### SendQuotationsRequestReceiveRefreshed

Отправить запрос на котировки, получить обновление

#### SendQuotationsRequestReceiveSnapshots

Отправить запрос на котировки, получить текущие значения

#### SendQuotationsBatchRequestReceiveRefreshedIndefinitely

Отправить запрос на батч котировки, получать обновления бесконечно и выписывать среднее количество поступивших обновлений и цен в них в секунду. Требует в appsettings.json секцию SendQuotationsBatchRequestReceiveRefreshedIndefinitely со списком инструментов и, опционально, контрагентов.

#### SendQuotation

Отправить котировку, дождаться сообщения с измененными ценами

#### CancelQuotation

Отправить отмену котировки, дождаться сообщения об отмене

#### ReceiveChats

Получить сообщение о начале чата, сообщение в чате, сообщение о завершении чата в фоновом режиме (в любом порядке)

#### SendChatsRequestAndReceiveChats

Отправить запрос на чаты за вчера и сегодня, получить подтверждение запроса, получить сообщения из чатов