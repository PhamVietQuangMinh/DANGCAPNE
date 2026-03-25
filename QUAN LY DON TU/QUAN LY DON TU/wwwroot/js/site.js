// Custom Smooth Animations & Interactions Script
// Automatically runs on every page that includes site.js

document.addEventListener("DOMContentLoaded", () => {
    // 1. Page Load Fade-In Transition
    document.body.classList.add("page-fade-in");

    // 2. Intersection Observer for Smooth Reveal on Scroll
    const observerOptions = {
        root: null,
        rootMargin: "0px",
        threshold: 0.1
    };

    const revealObserver = new IntersectionObserver((entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add("reveal-visible");
                observer.unobserve(entry.target); // Only animate once per render
            }
        });
    }, observerOptions);

    // Discover elements to animate on scroll
    const elementsToReveal = document.querySelectorAll('.glass-card, .data-table tbody tr, .stat-card, .info-box');
    elementsToReveal.forEach((el, index) => {
        if (!el.classList.contains('reveal-visible')) {
            // Table rows stagger from left, cards fade up
            if (el.tagName === 'TR') {
                el.style.transitionDelay = `${(index % 10) * 0.05}s`;
                el.classList.add('reveal-left');
            } else {
                el.classList.add('reveal-up');
            }
            revealObserver.observe(el);
        }
    });

    // 3. Premium Ripple Effect for Buttons and Interactive Elements
    const createRipple = (event) => {
        const button = event.currentTarget;
        const ripple = document.createElement("span");
        
        const diameter = Math.max(button.clientWidth, button.clientHeight);
        const radius = diameter / 2;
        
        // Ensure parent can contain absolute ripple
        if (window.getComputedStyle(button).position === 'static') {
            button.style.position = 'relative';
        }
        if (window.getComputedStyle(button).overflow !== 'hidden' && !button.classList.contains('nav-link')) {
            button.style.overflow = 'hidden';
        }

        const rect = button.getBoundingClientRect();
        
        // Handle keyboard enter triggering the click at (0,0)
        const isKeyboard = event.clientX === 0 && event.clientY === 0;
        const x = isKeyboard ? (rect.width/2) - radius : event.clientX - rect.left - radius;
        const y = isKeyboard ? (rect.height/2) - radius : event.clientY - rect.top - radius;

        ripple.style.width = ripple.style.height = `${diameter}px`;
        ripple.style.left = `${x}px`;
        ripple.style.top = `${y}px`;
        
        ripple.classList.add("ripple");
        // Dark ripple for light buttons
        if (button.classList.contains('btn-light') || button.classList.contains('btn-outline-primary') || button.classList.contains('nav-link')) {
            ripple.classList.add("ripple-dark");
        }

        const existingRipple = button.querySelector('.ripple');
        if (existingRipple) {
            existingRipple.remove();
        }

        button.appendChild(ripple);

        // Cleanup - Faster (300ms instead of 600ms)
        setTimeout(() => ripple.remove(), 350);
    };

    // Attach ripple to common interactive elements
    const rippleElements = document.querySelectorAll('.btn, .nav-link, .header-btn, .user-card, .action-btn');
    rippleElements.forEach(btn => {
        btn.addEventListener('mousedown', createRipple);
        btn.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' || e.key === ' ') {
                createRipple(e);
            }
        });
    });

    // 4. Smooth Page Exit Transition
    document.querySelectorAll('a[href]:not([target="_blank"]):not([href^="#"]):not([href^="javascript:"])').forEach(link => {
        link.addEventListener('click', (e) => {
            // Ignore if opening in new tab or specific actions
            if (e.ctrlKey || e.metaKey || e.shiftKey || e.defaultPrevented) return;
            
            const href = link.getAttribute('href');
            if (href && href !== '#' && !href.startsWith('javascript:')) {
                e.preventDefault();
                document.body.classList.remove("page-fade-in");
                document.body.classList.add("page-fade-out");
                
                setTimeout(() => {
                    window.location.href = href;
                }, 150); // Faster exit (150ms instead of 250ms)
            }
        });
    });

    // 5. Number Counting Animation for Statistics
    const animateValue = (obj, start, end, duration) => {
        let startTimestamp = null;
        const step = (timestamp) => {
            if (!startTimestamp) startTimestamp = timestamp;
            const progress = Math.min((timestamp - startTimestamp) / duration, 1);
            
            // Easing out
            const easeOut = 1 - Math.pow(1 - progress, 5);
            let current = progress === 1 ? end : start + (end - start) * easeOut;
            
            // Format number
            if (Number.isInteger(end)) {
                obj.innerHTML = Math.floor(current);
            } else {
                obj.innerHTML = current.toFixed(1);
            }
            
            if (progress < 1) {
                window.requestAnimationFrame(step);
            }
        };
        window.requestAnimationFrame(step);
    };

    const countObserver = new IntersectionObserver((entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const el = entry.target;
                const text = el.innerText.trim().replace(/,/g, '');
                const num = parseFloat(text);
                if (!isNaN(num) && num > 0) {
                    el.style.display = 'inline-block';
                    animateValue(el, 0, num, 1500); 
                }
                observer.unobserve(el);
            }
        });
    }, observerOptions);

    document.querySelectorAll('.stat-card h3, .progress-value, .count-number, .display-4').forEach(el => countObserver.observe(el));

    // 6. Premium 3D Tilt & Mouse Glow Effect
    const cards = document.querySelectorAll('.glass-card, .stat-card, .dashboard-stat-card, .user-card, .kanban-card');
    cards.forEach(card => {
        card.classList.add('glow-card', 'card-lift'); // Apply new premium styles automatically
        
        card.addEventListener('mousemove', (e) => {
            const rect = card.getBoundingClientRect();
            const x = e.clientX - rect.left; 
            const y = e.clientY - rect.top; 
            
            // Set CSS variables for the Glow Effect with smoother transition
            card.style.setProperty('--mouse-x', `${x}px`);
            card.style.setProperty('--mouse-y', `${y}px`);

            const centerX = rect.width / 2;
            const centerY = rect.height / 2;
            
            // Subtle rotation (max 2 degrees for professional feel)
            const rotateX = ((y - centerY) / centerY) * -2;
            const rotateY = ((x - centerX) / centerX) * 2;
            
            card.style.transform = `perspective(1000px) rotateX(${rotateX}deg) rotateY(${rotateY}deg) scale3d(1.01, 1.01, 1.01)`;
            card.style.transition = 'transform 0.05s linear'; // Faster tracking for tilt
            card.style.zIndex = '10';
        });
        
        card.addEventListener('mouseleave', () => {
            card.style.transform = 'perspective(1000px) rotateX(0deg) rotateY(0deg) scale3d(1, 1, 1)';
            card.style.transition = 'transform 0.5s cubic-bezier(0.25, 1, 0.5, 1)';
            card.style.zIndex = '1';
        });
    });

    // 7. Staggered entrance for all list containers
    document.querySelectorAll('.kanban-cards, .audit-list, .notif-list, .grid, .data-table tbody').forEach(container => {
        Array.from(container.children).forEach((child, i) => {
            child.style.opacity = '0';
            child.style.transform = 'translateY(10px)';
            child.style.transition = `all 0.4s cubic-bezier(0.16, 1, 0.3, 1) ${i * 0.05}s`;
            
            requestAnimationFrame(() => {
                setTimeout(() => {
                    child.style.opacity = '1';
                    child.style.transform = 'translateY(0)';
                }, 100);
            });
        });
    });

    // 8. Top Progress Bar & Button Loading Interceptors
    const showLoading = () => {
        let bar = document.getElementById('top-progress');
        if (!bar) {
            bar = document.createElement('div');
            bar.id = 'top-progress';
            document.body.appendChild(bar);
        }
        bar.style.width = '0%';
        bar.style.display = 'block';
        setTimeout(() => bar.style.width = '30%', 10);
        setTimeout(() => bar.style.width = '70%', 400);
    };

    // Intercept all meaningful link clicks
    document.querySelectorAll('a').forEach(link => {
        link.addEventListener('click', (e) => {
            const href = link.getAttribute('href');
            if (href && !href.startsWith('#') && !href.startsWith('javascript:') && !link.hasAttribute('target') && !e.ctrlKey && !e.metaKey) {
                showLoading();
            }
        });
    });

    // Intercept form submissions
    document.querySelectorAll('form').forEach(form => {
        form.addEventListener('submit', () => {
            showLoading();
            const submitBtn = form.querySelector('button[type="submit"]');
            if (submitBtn) {
                submitBtn.classList.add('btn-loading');
            }
        });
    });

    // End loading when page is fully loaded (for back/forward cache)
    window.addEventListener('pageshow', () => {
        const bar = document.getElementById('top-progress');
        if (bar) {
            bar.style.width = '100%';
            setTimeout(() => bar.style.display = 'none', 300);
        }
    });
});
