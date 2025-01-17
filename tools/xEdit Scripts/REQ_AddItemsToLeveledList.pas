unit AddItemsToLeveledList;

uses REQ_Util;

var
  newEditorID: String;
  leveledItem: IInterface;


function Initialize: Integer;
begin
  if not InputQuery('Enter', 'EditorID', newEditorID) then
    Result := -1;
end;


function Process(e: IInterface): Integer;
var
  entryArray, entry: IInterface;
begin
  if not Assigned(leveledItem) then begin
    leveledItem := AddRecordToFile(GetFile(e), 'LVLI');
    SetEditorID(leveledItem, newEditorID);
    entryArray := Add(leveledItem, 'Leveled List Entries', True);
    entry := ElementByIndex(entryArray, 0);
  end
  else begin
    entryArray := ElementByPath(leveledItem, 'Leveled List Entries');
    entry := ElementAssign(entryArray, HighInteger, nil, False);
  end;
  SetElementEditValues(entry, 'LVLO - Base Data\Level', '1');
  SetElementEditValues(entry, 'LVLO - Base Data\Reference', IntToHex(GetLoadOrderFormID(e), 8));
  SetElementEditValues(entry, 'LVLO - Base Data\Count', '1');
end;


function Finalize: Integer;
begin

end;

end.
