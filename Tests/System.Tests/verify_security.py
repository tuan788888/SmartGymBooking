from pathlib import Path
scope={};source=Path('Tests/System.Tests/verify_workflow.py').read_text(encoding='utf-8-sig');exec(source[:source.index('admin=Client()')],scope)
Client=scope['Client'];check=scope['check']
admin=Client();admin.login('admin@smartgym.vn','Admin@123')
customer=Client();customer.login('nguyenvana@gmail.com','Customer@123')
admin_actions={'CustomerManagement':['Edit','ToggleStatus','DeleteConfirmed'],'EmployeeManagement':['Create','Edit','ToggleStatus','DeleteConfirmed'],'PTManagement':['Create','Edit','ToggleStatus','DeleteConfirmed'],'PackageManagement':['Create','Edit','ToggleStatus','DeleteConfirmed'],'ServiceManagement':['Create','Edit','ToggleStatus','DeleteConfirmed'],'ExerciseManagement':['Create','Edit','DeleteConfirmed'],'PaymentManagement':['Confirm','MarkFailed'],'BookingManagement':['Confirm','Complete','Cancel'],'CheckInManagement':['CheckIn'],'PTScheduleManagement':['Create','Cancel','Complete']}
count=0
for controller,actions in admin_actions.items():
    for action in actions:
        admin.request('/'+controller+'/'+action,{'id':9223372036854775807},csrf=False)
        assert admin.code==400,(controller,action,admin.code)
        count+=1
for path in ['/Payment/Create','/Payment/Cancel','/Booking/Create','/Booking/Cancel','/Workout/Generate','/AIChat/Send','/AIChat/NewConversation','/Auth/Logout']:
    customer.request(path,{},csrf=False);assert customer.code==400,(path,customer.code);count+=1
for path in ['/Auth/Login','/Auth/Register']:
    anonymous=Client();anonymous.request(path,{},csrf=False);assert anonymous.code==400;count+=1
for path in ['/Workout/Generate','/Payment/Cancel','/Booking/Cancel','/AIChat/Send']:
    customer.request(path);assert customer.code in (404,405),(path,customer.code)
print('PASS: '+str(count)+' unsafe actions reject missing antiforgery token; customer mutation GETs return 404/405.')
