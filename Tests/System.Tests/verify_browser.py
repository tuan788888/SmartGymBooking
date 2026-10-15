from playwright.sync_api import sync_playwright
from pathlib import Path
base='http://127.0.0.1:5187'
widths=[1920,1366,768,390]
roles=[('admin@smartgym.vn','Admin@123',['/AdminDashboard','/CustomerManagement','/CustomerManagement/Details/1','/CustomerManagement/Edit/1','/EmployeeManagement','/EmployeeManagement/Create','/PTManagement','/PTManagement/Details/1','/PTManagement/Create','/PackageManagement','/PackageManagement/Create','/ServiceManagement','/ServiceManagement/Create','/ExerciseManagement','/ExerciseManagement/Create','/PaymentManagement','/BookingManagement','/CheckInManagement','/CheckInManagement/Create','/PTScheduleManagement','/PTScheduleManagement/Create']),('employee01@smartgym.vn','Employee@123',['/EmployeeDashboard','/CustomerManagement','/PaymentManagement','/BookingManagement','/CheckInManagement','/PTScheduleManagement']),('nguyenvana@gmail.com','Customer@123',['/','/Package','/Payment/History','/Membership','/Booking','/Booking/Create','/MyPTSchedule','/Workout','/Workout/Details/2','/AIChat','/Payment/Checkout/24'])]
with sync_playwright() as p:
    browser=p.chromium.launch(channel='msedge',headless=True)
    checks=0
    for email,password,paths in roles:
        context=browser.new_context();page=context.new_page();errors=[]
        page.on('pageerror',lambda e:errors.append(str(e)))
        page.goto(base+'/Auth/Login');page.locator('#Email').fill(email);page.locator('#Password').fill(password);page.locator('button[type=submit]').click();page.wait_for_load_state('networkidle')
        cookies=context.cookies();auth=next(c for c in cookies if c['name']=='SmartGymBooking.Auth');assert auth['httpOnly'] and auth['sameSite']=='Lax'
        if email.startswith('nguyen'):
            paths=[path for path in paths if not path.startswith('/Payment/Checkout')]
            # Existing seeded customer owns a checkout; discover its QR link from history.
            page.goto(base+'/Payment/History')
            qr=page.locator('a[href^="/Payment/Checkout/"]').first
            if qr.count():paths.append(qr.get_attribute('href'))
            forbidden=['/AdminDashboard','/EmployeeDashboard','/EmployeeManagement','/PTManagement','/PaymentManagement','/BookingManagement','/ExerciseManagement','/DatabaseTest']
        elif email.startswith('employee'):
            forbidden=['/AdminDashboard','/EmployeeManagement','/PackageManagement','/ExerciseManagement','/ServiceManagement','/CustomerManagement/Edit/1','/CustomerManagement/Delete/1','/DatabaseTest']
        else:forbidden=['/EmployeeDashboard']
        for path in forbidden:
            page.goto(base+path);assert '/Auth/AccessDenied' in page.url,(email,path,page.url)
        for width in widths:
            page.set_viewport_size({'width':width,'height':960})
            for path in paths:
                response=page.goto(base+path);page.wait_for_load_state('networkidle')
                assert response.status==200,(path,response.status)
                assert page.evaluate('document.documentElement.scrollWidth<=innerWidth+1'),(email,width,path,'horizontal overflow')
                assert page.locator('table').evaluate_all('(tables)=>tables.every(t=>!!t.closest(".table-responsive"))'),('Unwrapped table',path)
                checks+=1
            print('PASS: responsive '+email+' '+str(width),flush=True)
        if email.startswith('admin'):
            page.goto(base+'/PTManagement')
            page.evaluate("""() => { const img=document.createElement('img');img.setAttribute('data-pt-avatar','');img.src='/images/pts/missing-b33.png';img.alt='QA Avatar';document.querySelector('main').append(img); }""")
            # Reload the site script to exercise the same fallback on a missing image.
            page.add_script_tag(url=base+'/js/site.js')
            page.wait_for_function("!document.querySelector('img[alt=\"QA Avatar\"]') && !!document.querySelector('[aria-label=\"QA Avatar\"]')")
        assert not errors,errors
        context.close()
    # Public error pages are also readable at mobile width.
    page=browser.new_page(viewport={'width':390,'height':844})
    for path,status in [('/not-a-real-page',404),('/Error/StatusCode/403',403),('/Error/ServerError',500)]:
        response=page.goto(base+path);assert response.status==status
        assert page.evaluate('document.documentElement.scrollWidth<=innerWidth+1')
    browser.close()
print('PASS: '+str(checks)+' responsive page/viewport checks, direct role URLs, wrapped tables, HttpOnly/Lax cookies, missing-avatar fallback and error status codes.')
