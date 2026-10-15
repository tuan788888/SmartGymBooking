import base64,datetime,html,http.cookiejar,json,re,subprocess,urllib.parse,urllib.request,urllib.error,uuid
from pathlib import Path
BASE='http://127.0.0.1:5187'
TAG='B33QA'+datetime.datetime.now().strftime('%Y%m%d%H%M%S')
DLL='Tests/System.Tests/bin/Debug/net10.0/System.Tests.dll'
def db(command='snapshot'):
    result=subprocess.run(['dotnet',DLL,command,TAG],capture_output=True,text=True,encoding='utf-8',check=True)
    return json.loads(result.stdout.strip().splitlines()[-1]) if command=='snapshot' else result.stdout
class Client:
    def __init__(self): self.client=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(http.cookiejar.CookieJar()));self.page='';self.url=''
    def request(self,path,fields=None,file=None,csrf=True):
        data=None;headers={}
        if fields is not None:
            fields={k:str(v) for k,v in fields.items()}
            if csrf: fields['__RequestVerificationToken']=re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"',self.page).group(1)
            if file:
                boundary='----B33'+uuid.uuid4().hex;parts=[]
                for key,value in fields.items():parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="{key}"\r\n\r\n{value}\r\n'.encode())
                parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="AvatarFile"; filename="avatar.png"\r\nContent-Type: image/png\r\n\r\n'.encode()+file+b'\r\n')
                parts.append(f'--{boundary}--\r\n'.encode());data=b''.join(parts);headers['Content-Type']='multipart/form-data; boundary='+boundary
            else:data=urllib.parse.urlencode(fields).encode()
        try:r=self.client.open(urllib.request.Request(BASE+path,data,headers))
        except urllib.error.HTTPError as error:r=error
        self.code=r.code;self.url=r.geturl();self.page=html.unescape(r.read().decode(errors="replace"));return self.page
    def login(self,email,password):
        self.request('/Auth/Login');self.request('/Auth/Login',{'Email':email,'Password':password});assert self.code==200 and '/Auth/Login' not in self.url,self.page
    def post(self,path,fields={},file=None):
        if not re.search(r'name="__RequestVerificationToken"',self.page):self.request('/Package')
        return self.request(path,fields,file)
def check(condition,name):
    assert condition,name
    print('PASS: '+name,flush=True)
admin=Client();admin.login('admin@smartgym.vn','Admin@123')
customer=Client()
customer.request('/health');check(json.loads(customer.page)['status']=='Healthy','anonymous health')
customer.request('/not-a-real-page');check(customer.code==404 and 'Không tìm thấy trang' in customer.page,'404 page/status')
customer.request('/Error/ServerError');check(customer.code==500 and 'Hệ thống gặp sự cố' in customer.page,'500 page/status')
customer.request('/Auth/Register')
profile={'FullName':TAG+' Customer','Email':TAG.lower()+'@example.test','Password':'B33Test@123','ConfirmPassword':'B33Test@123','Gender':'MALE','DateOfBirth':'2003-05-10','Role':'ADMIN'}
future=profile|{'DateOfBirth':'2099-01-01'}
customer.post('/Auth/Register',future);check('Ngày sinh phải trước ngày hiện tại' in customer.page and not db()['Customers'],'future DOB rejected')
customer.post('/Auth/Register',profile);customer.login(profile['Email'],profile['Password'])
s=db();cid=s['Customers'][0]['CustomerId'];check(s['Customers'][0]['Role']=='CUSTOMER','registration ignores forged ADMIN role')
for path,text in [('/Booking','Bạn chưa có lịch đặt'),('/Payment/History','Bạn chưa có giao dịch'),('/Membership','Membership'),('/MyPTSchedule','lịch'),('/Workout','giáo án'),('/AIChat?newChat=true','Hãy thử hỏi')]:
    customer.request(path);check(customer.code==200 and text.lower() in customer.page.lower(),'empty state '+path)
admin.request('/CustomerManagement?search='+TAG);check(TAG in admin.page,'customer search')
admin.request('/CustomerManagement/Details/'+str(cid));check(admin.code==200,'customer details')
admin.post('/CustomerManagement/Edit',{'CustomerId':cid,'FullName':profile['FullName'],'DateOfBirth':'2003-05-10','Gender':'MALE','Height':175,'Weight':70,'FitnessGoal':'MUSCLE_GAIN','ExperienceLevel':'BEGINNER','ActivityLevel':'MODERATE','PreferredSessionsPerWeek':4,'PreferredSessionMinutes':60})
# Create, edit and lock/delete employee; GET Delete must not mutate.
emp={'FullName':TAG+' Employee','Email':TAG.lower()+'.employee@example.test','Position':'QA','IsActive':'true','Password':'B33Test@123'}
admin.post('/EmployeeManagement/Create',emp);eid=db()['Employees'][0]['EmployeeId']
admin.post('/EmployeeManagement/Edit',emp|{'EmployeeId':eid,'Position':'QA updated','Password':''})
admin.post('/EmployeeManagement/ToggleStatus/'+str(eid));check(not db()['Employees'][0]['IsActive'],'employee edit/lock')
admin.request('/EmployeeManagement/Delete/'+str(eid));check(len(db()['Employees'])==1,'GET Delete does not delete')
admin.post('/EmployeeManagement/DeleteConfirmed/'+str(eid));check(not db()['Employees'],'employee delete')
pkg={'Name':TAG+' Package','Price':1000,'DurationDays':30,'IncludesPT':'true','IsActive':'true'}
admin.post('/PackageManagement/Create',pkg);pid=db()['Packages'][0]['PackageId']
admin.post('/PackageManagement/Edit',pkg|{'PackageId':pid,'Description':'QA edited'})
admin.post('/PackageManagement/ToggleStatus/'+str(pid));check(not db()['Packages'][0]['IsActive'],'package inactive')
admin.post('/PackageManagement/ToggleStatus/'+str(pid))
svc={'Name':TAG+' Service','Description':'QA','IsActive':'true'}
admin.post('/ServiceManagement/Create',svc);sid=db()['Services'][0]['ServiceId']
admin.post('/ServiceManagement/Edit',svc|{'ServiceId':sid,'Description':'QA edited'})
admin.post('/ServiceManagement/ToggleStatus/'+str(sid));check(not db()['Services'][0]['IsActive'],'service inactive')
admin.post('/ServiceManagement/ToggleStatus/'+str(sid))
pt={'FullName':TAG+' PT','Email':TAG.lower()+'.pt@example.test','Specialization':'QA','Experience':1,'Status':'ACTIVE'}
png=base64.b64decode('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=')
admin.post('/PTManagement/Create',pt,png);ptid=db()['PTs'][0]['Ptid'];avatar=db()['PTs'][0]['Avatar']
admin.request(avatar);check(admin.code==200,'PT image upload/serve')
admin.request('/PTManagement');admin.post('/PTManagement/Edit',pt|{'PTId':ptid,'Description':'QA edited'})
# A standalone exercise can be edited and deleted safely.
exercise={'Name':TAG+' Exercise','Difficulty':'BEGINNER','MuscleGroup':'CHEST','VideoUrl':'https://example.com/video'}
admin.post('/ExerciseManagement/Create',exercise);xid=db()['Exercises'][0]['ExerciseId']
admin.post('/ExerciseManagement/Edit',exercise|{'ExerciseId':xid,'Instructions':'QA edited'})
admin.request('/ExerciseManagement/Edit/'+str(xid));check('https://example.com/video' in admin.page,'exercise video/edit')
# Full customer purchase flow, repeat confirmation must create one membership.
customer.request('/Package');customer.post('/Payment/Create',{'packageId':pid});paymentid=db()['Payments'][0]['PaymentId']
check('/Payment/Checkout/' in customer.url and 'bank-qr.jpg' in customer.page,'customer payment PENDING and QR')
admin.request('/PaymentManagement');admin.post('/PaymentManagement/Confirm/'+str(paymentid));admin.post('/PaymentManagement/Confirm/'+str(paymentid))
s=db();check(s['Payments'][0]['Status']=='PAID' and len(s['Memberships'])==1,'PAID creates one membership; repeat confirm blocked')
customer.request('/Membership');check('ACTIVE' in customer.page.upper(),'customer membership')
now=datetime.datetime.now();start=now+datetime.timedelta(days=1);end=start+datetime.timedelta(hours=1)
time=lambda d:d.strftime('%Y-%m-%dT%H:%M')
booking={'ServiceId':sid,'StartTime':time(start),'EndTime':time(end)}
customer.post('/Booking/Create',booking);bid=db()['Bookings'][0]['BookingId']
customer.post('/Booking/Create',booking|{'StartTime':time(start+datetime.timedelta(minutes=20))});check(len(db()['Bookings'])==1 and 'trùng thời gian' in customer.page,'booking overlap rejected')
customer.post('/Booking/Create',booking|{'StartTime':time(now-datetime.timedelta(hours=1)),'EndTime':time(now)});check(len(db()['Bookings'])==1,'past booking rejected')
customer.post('/Booking/Create',booking|{'StartTime':time(start+datetime.timedelta(days=60)),'EndTime':time(end+datetime.timedelta(days=60))});check(len(db()['Bookings'])==1,'booking outside membership rejected')
admin.post('/BookingManagement/Confirm/'+str(bid));check(db()['Bookings'][0]['Status']=='CONFIRMED','booking confirm')
admin.post('/CheckInManagement/CheckIn',{'bookingId':bid});admin.post('/CheckInManagement/CheckIn',{'bookingId':bid});check(len(db()['CheckIns'])==1,'confirmed booking check-in exactly once')
schedule={'PTId':ptid,'CustomerId':cid,'StartTime':time(start),'EndTime':time(end)}
admin.post('/PTScheduleManagement/Create',schedule);check(len(db()['Schedules'])==1,'PT schedule within rolling 14 days')
admin.post('/PTScheduleManagement/Create',schedule|{'StartTime':time(start+datetime.timedelta(minutes=20))});check(len(db()['Schedules'])==1 and 'trùng' in admin.page,'PT/customer overlap rejected')
admin.post('/PTScheduleManagement/Create',schedule|{'StartTime':time(now+datetime.timedelta(days=15)),'EndTime':time(now+datetime.timedelta(days=15,hours=1))});check(len(db()['Schedules'])==1 and '14 ngày' in admin.page,'PT schedule beyond 14 days rejected')
customer.request('/MyPTSchedule');check(TAG+' PT' in customer.page,'customer sees own PT schedule')
customer.post('/Workout/Generate');s=db();planid=s['Plans'][0]['WorkoutPlanId'];check(s['Plans'][0]['ModelName']=='RandomForest' and s['Plans'][0]['Exercises']>0,'ML ONNX generates plan/exercises')
customer.post('/Workout/Generate');check(len(db()['Plans'])==1,'ML daily limit')
customer.request('/AIChat?newChat=true');customer.post('/AIChat/Send',{'Message':'Giáo án hiện tại của tôi là gì?'});s=db();convid=s['Conversations'][0]['AiconversationId'];check(s['Conversations'][0]['Roles']==['USER','ASSISTANT'] and s['Plans'][0]['PredictedPlanType'] in customer.page,'DEMO chat saves history and workout context')
# Another customer cannot read/write these resources.
other=Client();other.login('tranthib@gmail.com','Customer@123')
for path in ['/Payment/Checkout/'+str(paymentid),'/Workout/Details/'+str(planid),'/AIChat?conversationId='+str(convid)]:
    other.request(path);check(other.code==404,'ownership '+path)
other.request('/Package');other.post('/Booking/Cancel/'+str(bid));check(other.code==404 and db()['Bookings'][0]['Status']=='CONFIRMED','ownership booking cancel')
other.request('/Package');other.post('/Payment/Cancel/'+str(paymentid));check(other.code==404,'ownership payment cancel')
other.request('/Package');other.post('/AIChat/Send',{'ConversationId':convid,'Message':'foreign'});check(other.code==404,'ownership chat send')
other.request('/MyPTSchedule');check(TAG+' PT' not in other.page,'PT list owner filtering')
# Delete guards preserve all transaction history.
for controller,identity in [('CustomerManagement',cid),('PackageManagement',pid),('ServiceManagement',sid),('PTManagement',ptid)]:
    admin.request('/'+controller);admin.post('/'+controller+'/DeleteConfirmed/'+str(identity));check('Không thể xóa' in admin.page,'history delete guard '+controller)
# Complete only ended sessions; make explicitly owned QA fixtures represent ended sessions.
admin.post('/BookingManagement/Complete/'+str(bid));check(db()['Bookings'][0]['Status']=='CONFIRMED','cannot complete future booking')
db('age');admin.post('/BookingManagement/Complete/'+str(bid));scheduleid=db()['Schedules'][0]['PtscheduleId'];admin.post('/PTScheduleManagement/Complete/'+str(scheduleid));s=db();check(s['Bookings'][0]['Status']=='COMPLETED' and s['Schedules'][0]['Status']=='COMPLETED','ended booking/PT completion')
# Lock immediately revokes existing customer cookie and uses generic login errors.
admin.request('/CustomerManagement');admin.post('/CustomerManagement/ToggleStatus/'+str(cid));customer.request('/Workout');check('/Auth/Login' in customer.url,'locked account cookie revoked')
locked=Client();locked.request('/Auth/Login');locked.post('/Auth/Login',{'Email':profile['Email'],'Password':profile['Password']});check('Email hoặc mật khẩu không chính xác' in locked.page and 'Tài khoản đã bị khóa' not in locked.page,'locked login generic error')
# Delete unused exercise and leave QA history safely isolated and inactive.
admin.request('/ExerciseManagement');admin.post('/ExerciseManagement/DeleteConfirmed/'+str(xid));check(not db()['Exercises'],'unused exercise delete')
admin.post('/PTManagement/ToggleStatus/'+str(ptid));admin.post('/PackageManagement/ToggleStatus/'+str(pid));admin.post('/ServiceManagement/ToggleStatus/'+str(sid))
Path('obj/b33-qa-tag.txt').write_text(TAG)
print('ALL WORKFLOW CHECKS PASSED: '+TAG,flush=True)
